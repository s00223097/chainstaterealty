// SPDX-License-Identifier: MIT
pragma solidity ^0.8.19;

import "@openzeppelin/contracts/access/Ownable.sol";
import "@openzeppelin/contracts/security/ReentrancyGuard.sol";
import "@openzeppelin/contracts/token/ERC1155/IERC1155.sol";
import "./PropertyToken.sol";

contract RevenueDistribution is Ownable, ReentrancyGuard {
    PropertyToken public propertyToken;
    
    // Revenue distribution period structure
    struct RevenuePeriod {
        uint256 tokenId;
        uint256 totalAmount;
        uint256 remainingAmount;
        uint256 totalShares;
        uint256 amountPerShare;
        uint256 startTime;
        uint256 endTime;
        mapping(address => bool) claimed;
    }
    
    // Property expense structure
    struct PropertyExpense {
        uint256 tokenId;
        string description;
        uint256 amount;
        uint256 timestamp;
        bool paid;
    }
    
    // Property maintenance reserve
    struct MaintenanceReserve {
        uint256 tokenId;
        uint256 balance;
        uint256 targetAmount;
        uint256 monthlyContribution;
        uint256 lastContribution;
    }
    
    // Mappings
    mapping(uint256 => RevenuePeriod) public revenuePeriods;
    mapping(uint256 => PropertyExpense[]) public propertyExpenses;
    mapping(uint256 => MaintenanceReserve) public maintenanceReserves;
    
    uint256 public nextPeriodId = 1;
    uint256 public managementFeeRate = 500; // 5% in basis points
    
    // Events
    event RevenuePeriodCreated(uint256 indexed periodId, uint256 indexed tokenId, uint256 amount, uint256 startTime, uint256 endTime);
    event RevenueClaimed(uint256 indexed periodId, address indexed claimer, uint256 amount);
    event ExpenseAdded(uint256 indexed tokenId, string description, uint256 amount);
    event ExpensePaid(uint256 indexed tokenId, uint256 expenseIndex);
    event MaintenanceReserveUpdated(uint256 indexed tokenId, uint256 targetAmount, uint256 monthlyContribution);
    event MaintenanceReserveContribution(uint256 indexed tokenId, uint256 amount);
    event MaintenanceReserveWithdrawal(uint256 indexed tokenId, uint256 amount, string purpose);
    
    constructor(address _propertyTokenAddress) {
        propertyToken = PropertyToken(_propertyTokenAddress);
    }
    
    /**
     * @dev Create a new revenue distribution period
     * @param tokenId Property token ID
     * @param amount Total amount to distribute
     * @param durationDays Duration of the period in days
     */
    function createRevenuePeriod(uint256 tokenId, uint256 amount, uint256 durationDays) external payable onlyOwner {
        require(amount > 0, "Amount must be greater than zero");
        require(durationDays > 0, "Duration must be greater than zero");
        require(msg.value == amount, "Sent value must match amount");
        
        // Get property details
        (uint256 totalShares,,,,) = propertyToken.getPropertyDetails(tokenId);
        require(totalShares > 0, "Invalid property");
        
        // Calculate management fee
        uint256 managementFee = (amount * managementFeeRate) / 10000;
        uint256 distributionAmount = amount - managementFee;
        
        // Calculate maintenance reserve contribution
        MaintenanceReserve storage reserve = maintenanceReserves[tokenId];
        uint256 reserveContribution = 0;
        
        if (reserve.targetAmount > 0) {
            // Check if it's time for a new contribution
            uint256 oneMonth = 30 days;
            if (block.timestamp >= reserve.lastContribution + oneMonth) {
                reserveContribution = reserve.monthlyContribution;
                if (reserve.balance + reserveContribution > reserve.targetAmount) {
                    reserveContribution = reserve.targetAmount - reserve.balance;
                }
                
                if (reserveContribution > 0) {
                    reserve.balance += reserveContribution;
                    reserve.lastContribution = block.timestamp;
                    
                    emit MaintenanceReserveContribution(tokenId, reserveContribution);
                }
            }
        }
        
        // Final distribution amount after fees and reserve
        uint256 finalDistributionAmount = distributionAmount - reserveContribution;
        
        // Calculate amount per share
        uint256 amountPerShare = finalDistributionAmount / totalShares;
        
        // Create period
        uint256 periodId = nextPeriodId++;
        RevenuePeriod storage period = revenuePeriods[periodId];
        period.tokenId = tokenId;
        period.totalAmount = finalDistributionAmount;
        period.remainingAmount = finalDistributionAmount;
        period.totalShares = totalShares;
        period.amountPerShare = amountPerShare;
        period.startTime = block.timestamp;
        period.endTime = block.timestamp + (durationDays * 1 days);
        
        emit RevenuePeriodCreated(periodId, tokenId, finalDistributionAmount, period.startTime, period.endTime);
    }
    
    /**
     * @dev Claim revenue for a distribution period
     * @param periodId Period ID
     */
    function claimRevenue(uint256 periodId) external nonReentrant {
        RevenuePeriod storage period = revenuePeriods[periodId];
        require(period.startTime > 0, "Period does not exist");
        require(block.timestamp >= period.startTime, "Period not started yet");
        require(!period.claimed[msg.sender], "Already claimed for this period");
        
        // Get user's share balance
        uint256 userShares = propertyToken.balanceOf(msg.sender, period.tokenId);
        require(userShares > 0, "No shares owned");
        
        // Calculate user's portion
        uint256 userAmount = userShares * period.amountPerShare;
        require(userAmount > 0, "No revenue to claim");
        require(period.remainingAmount >= userAmount, "Not enough funds left in period");
        
        // Mark as claimed and update remaining amount
        period.claimed[msg.sender] = true;
        period.remainingAmount -= userAmount;
        
        // Transfer revenue to user
        payable(msg.sender).transfer(userAmount);
        
        emit RevenueClaimed(periodId, msg.sender, userAmount);
    }
    
    /**
     * @dev Add a new property expense
     * @param tokenId Property token ID
     * @param description Expense description
     * @param amount Expense amount
     */
    function addExpense(uint256 tokenId, string memory description, uint256 amount) external onlyOwner {
        require(amount > 0, "Amount must be greater than zero");
        
        propertyExpenses[tokenId].push(PropertyExpense({
            tokenId: tokenId,
            description: description,
            amount: amount,
            timestamp: block.timestamp,
            paid: false
        }));
        
        emit ExpenseAdded(tokenId, description, amount);
    }
    
    /**
     * @dev Pay a property expense from maintenance reserve
     * @param tokenId Property token ID
     * @param expenseIndex Index of the expense in the array
     */
    function payExpense(uint256 tokenId, uint256 expenseIndex) external onlyOwner nonReentrant {
        PropertyExpense[] storage expenses = propertyExpenses[tokenId];
        require(expenseIndex < expenses.length, "Invalid expense index");
        PropertyExpense storage expense = expenses[expenseIndex];
        require(!expense.paid, "Expense already paid");
        
        MaintenanceReserve storage reserve = maintenanceReserves[tokenId];
        require(reserve.balance >= expense.amount, "Insufficient reserve balance");
        
        // Update expense and reserve
        expense.paid = true;
        reserve.balance -= expense.amount;
        
        // Transfer expense amount to owner/manager
        payable(owner()).transfer(expense.amount);
        
        emit ExpensePaid(tokenId, expenseIndex);
    }
    
    /**
     * @dev Set up maintenance reserve for a property
     * @param tokenId Property token ID
     * @param targetAmount Target reserve amount
     * @param monthlyContribution Monthly contribution amount
     */
    function setupMaintenanceReserve(uint256 tokenId, uint256 targetAmount, uint256 monthlyContribution) external onlyOwner {
        require(targetAmount > 0, "Target amount must be greater than zero");
        require(monthlyContribution > 0, "Monthly contribution must be greater than zero");
        
        MaintenanceReserve storage reserve = maintenanceReserves[tokenId];
        reserve.tokenId = tokenId;
        reserve.targetAmount = targetAmount;
        reserve.monthlyContribution = monthlyContribution;
        
        if (reserve.lastContribution == 0) {
            reserve.lastContribution = block.timestamp;
        }
        
        emit MaintenanceReserveUpdated(tokenId, targetAmount, monthlyContribution);
    }
    
    /**
     * @dev Make additional contribution to maintenance reserve
     * @param tokenId Property token ID
     */
    function contributeToReserve(uint256 tokenId) external payable {
        require(msg.value > 0, "Contribution must be greater than zero");
        
        MaintenanceReserve storage reserve = maintenanceReserves[tokenId];
        reserve.balance += msg.value;
        
        emit MaintenanceReserveContribution(tokenId, msg.value);
    }
    
    /**
     * @dev Withdraw from maintenance reserve for emergency repairs
     * @param tokenId Property token ID
     * @param amount Amount to withdraw
     * @param purpose Purpose of withdrawal
     */
    function withdrawFromReserve(uint256 tokenId, uint256 amount, string memory purpose) external onlyOwner nonReentrant {
        MaintenanceReserve storage reserve = maintenanceReserves[tokenId];
        require(reserve.balance >= amount, "Insufficient reserve balance");
        
        reserve.balance -= amount;
        payable(owner()).transfer(amount);
        
        emit MaintenanceReserveWithdrawal(tokenId, amount, purpose);
    }
    
    /**
     * @dev Get maintenance reserve details
     * @param tokenId Property token ID
     */
    function getReserveDetails(uint256 tokenId) external view returns (uint256 balance, uint256 targetAmount, uint256 monthlyContribution) {
        MaintenanceReserve storage reserve = maintenanceReserves[tokenId];
        return (reserve.balance, reserve.targetAmount, reserve.monthlyContribution);
    }
    
    /**
     * @dev Update management fee rate
     * @param newFeeRate New fee rate in basis points
     */
    function updateManagementFeeRate(uint256 newFeeRate) external onlyOwner {
        require(newFeeRate <= 2000, "Fee cannot exceed 20%");
        managementFeeRate = newFeeRate;
    }
    
    /**
     * @dev Withdraw management fees
     */
    function withdrawManagementFees() external onlyOwner {
        uint256 balance = address(this).balance;
        require(balance > 0, "No balance to withdraw");
        payable(owner()).transfer(balance);
    }
} 