// SPDX-License-Identifier: MIT
pragma solidity ^0.8.19;

import "@openzeppelin/contracts/token/ERC1155/ERC1155.sol";
import "@openzeppelin/contracts/access/Ownable.sol";
import "@openzeppelin/contracts/utils/Strings.sol";

contract PropertyToken is ERC1155, Ownable {
    using Strings for uint256;
    
    // Property token data structure
    struct PropertyData {
        uint256 tokenId;              
        uint256 totalShares;          
        uint256 availableShares;      // Purchasable shares
        uint256 pricePerShare;        // In wei
        string propertyURI;           
        bool active;                  // Whether the property is active for trading
    }
    
    // Property token registry
    mapping(uint256 => PropertyData) public properties;
    
    // Track share ownership per address per property
    mapping(uint256 => mapping(address => uint256)) public shareBalances;
    
    // Next token ID to be assigned
    uint256 private _nextTokenId = 1;
    
    // Events
    event PropertyCreated(uint256 indexed tokenId, uint256 totalShares, uint256 pricePerShare, string propertyURI);
    event SharesPurchased(address indexed buyer, uint256 indexed tokenId, uint256 amount, uint256 cost);
    event SharesSold(address indexed seller, uint256 indexed tokenId, uint256 amount, uint256 payment);
    event PropertyActivated(uint256 indexed tokenId, bool active);
    
    constructor() ERC1155("") {}
    
    /**
     * @dev Creates a new property token with fractional shares
     * @param totalShares Total number of shares for this property
     * @param pricePerShare Initial price per share in wei
     * @param propertyURI URI pointing to property metadata
     */
    function createProperty(
        uint256 totalShares,
        uint256 pricePerShare,
        string memory propertyURI
    ) external onlyOwner {
        require(totalShares > 0, "Shares must be greater than zero");
        require(pricePerShare > 0, "Price must be greater than zero");
        require(bytes(propertyURI).length > 0, "URI cannot be empty");
        
        uint256 tokenId = _nextTokenId++;
        
        properties[tokenId] = PropertyData({
            tokenId: tokenId,
            totalShares: totalShares,
            availableShares: totalShares,
            pricePerShare: pricePerShare,
            propertyURI: propertyURI,
            active: true
        });
        
        // Mint all shares to the contract itself initially
        _mint(address(this), tokenId, totalShares, "");
        
        emit PropertyCreated(tokenId, totalShares, pricePerShare, propertyURI);
    }
    
    /**
     * @dev Allows users to buy shares of a property
     * @param tokenId The property token ID
     * @param amount Number of shares to purchase
     */
    function purchaseShares(uint256 tokenId, uint256 amount) external payable {
        PropertyData storage property = properties[tokenId];
        require(property.active, "Property is not active");
        require(property.availableShares >= amount, "Not enough shares available");
        
        uint256 cost = amount * property.pricePerShare;
        require(msg.value >= cost, "Insufficient payment");
        
        // Transfer shares from contract to buyer
        _safeTransferFrom(address(this), msg.sender, tokenId, amount, "");
        
        // Update available shares and user's balance
        property.availableShares -= amount;
        shareBalances[tokenId][msg.sender] += amount;
        
        // Return excess payment if any
        if (msg.value > cost) {
            payable(msg.sender).transfer(msg.value - cost);
        }
        
        emit SharesPurchased(msg.sender, tokenId, amount, cost);
    }
    
    /**
     * @dev Allows users to sell their shares back to the contract
     * @param tokenId The property token ID
     * @param amount Number of shares to sell
     */
    function sellShares(uint256 tokenId, uint256 amount) external {
        PropertyData storage property = properties[tokenId];
        require(property.active, "Property is not active");
        require(balanceOf(msg.sender, tokenId) >= amount, "Not enough shares owned");
        
        uint256 payment = amount * property.pricePerShare;
        
        // Transfer shares from seller to contract
        _safeTransferFrom(msg.sender, address(this), tokenId, amount, "");
        
        // Update available shares and user's balance
        property.availableShares += amount;
        shareBalances[tokenId][msg.sender] -= amount;
        
        // Transfer payment to seller
        payable(msg.sender).transfer(payment);
        
        emit SharesSold(msg.sender, tokenId, amount, payment);
    }
    
    /**
     * @dev Set property active status
     * @param tokenId The property token ID
     * @param active New active status
     */
    function setPropertyActive(uint256 tokenId, bool active) external onlyOwner {
        require(properties[tokenId].tokenId == tokenId, "Property does not exist");
        properties[tokenId].active = active;
        emit PropertyActivated(tokenId, active);
    }
    
    /**
     * @dev Update the price per share for a property
     * @param tokenId The property token ID
     * @param newPrice New price per share in wei
     */
    function updatePropertyPrice(uint256 tokenId, uint256 newPrice) external onlyOwner {
        require(properties[tokenId].tokenId == tokenId, "Property does not exist");
        require(newPrice > 0, "Price must be greater than zero");
        properties[tokenId].pricePerShare = newPrice;
    }
    
    /**
     * @dev Override URI function to return property-specific URI
     */
    function uri(uint256 tokenId) public view override returns (string memory) {
        require(properties[tokenId].tokenId == tokenId, "Property does not exist");
        return properties[tokenId].propertyURI;
    }
    
    /**
     * @dev Update the metadata URI for a property
     * @param tokenId The property token ID
     * @param newURI New URI for property metadata
     */
    function setPropertyURI(uint256 tokenId, string memory newURI) external onlyOwner {
        require(properties[tokenId].tokenId == tokenId, "Property does not exist");
        require(bytes(newURI).length > 0, "URI cannot be empty");
        properties[tokenId].propertyURI = newURI;
    }
    
    /**
     * @dev Returns all properties owned by an address
     * @param owner Address to check
     * @return Array of token IDs and share amounts
     */
    function getOwnedProperties(address owner) external view returns (uint256[] memory, uint256[] memory) {
        uint256 ownedCount = 0;
        
        // Count properties owned by the address
        for (uint256 i = 1; i < _nextTokenId; i++) {
            if (balanceOf(owner, i) > 0) {
                ownedCount++;
            }
        }
        
        // Create arrays for token IDs and their amounts
        uint256[] memory tokenIds = new uint256[](ownedCount);
        uint256[] memory amounts = new uint256[](ownedCount);
        
        // Fill arrays with data
        uint256 index = 0;
        for (uint256 i = 1; i < _nextTokenId; i++) {
            uint256 balance = balanceOf(owner, i);
            if (balance > 0) {
                tokenIds[index] = i;
                amounts[index] = balance;
                index++;
            }
        }
        
        return (tokenIds, amounts);
    }
    
    /**
     * @dev Get full details for a property token.
     * @param tokenId The property token ID.
     * @return totalShares Total number of shares minted for the property.
     * @return availableShares Number of shares still owned by the contract (unsold).
     * @return pricePerShare Current price per share in wei.
     * @return propertyURI Metadata URI for the property.
     * @return active Whether the property is active for trading.
     */
    function getPropertyDetails(uint256 tokenId) external view returns (
        uint256 totalShares,
        uint256 availableShares,
        uint256 pricePerShare,
        string memory propertyURI,
        bool active
    ) {
        PropertyData memory property = properties[tokenId];
        require(property.tokenId == tokenId, "Property does not exist");
        
        return (
            property.totalShares,
            property.availableShares,
            property.pricePerShare,
            property.propertyURI,
            property.active
        );
    }
    
    /**
     * @dev Withdraw contract balance (for owner only)
     */
    function withdraw() external onlyOwner {
        uint256 balance = address(this).balance;
        require(balance > 0, "No balance to withdraw");
        payable(owner()).transfer(balance);
    }
} 