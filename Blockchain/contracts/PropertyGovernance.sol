// SPDX-License-Identifier: MIT
pragma solidity ^0.8.19;

import "@openzeppelin/contracts/access/Ownable.sol";
import "@openzeppelin/contracts/utils/Counters.sol";
import "./PropertyToken.sol";

contract PropertyGovernance is Ownable {
    using Counters for Counters.Counter;
    
    PropertyToken public propertyToken;
    
    enum ProposalType { 
        MAINTENANCE,
        IMPROVEMENT,
        POLICY_CHANGE,
        SALE,
        OTHER
    }
    
    enum ProposalStatus {
        ACTIVE,
        APPROVED,
        REJECTED,
        EXECUTED,
        CANCELLED
    }
    
    struct Proposal {
        uint256 proposalId;
        uint256 tokenId;
        address proposer;
        string title;
        string description;
        uint256 amount;
        ProposalType proposalType;
        ProposalStatus status;
        uint256 startTime;
        uint256 endTime;
        uint256 yesVotes;
        uint256 noVotes;
        bool executed;
        mapping(address => bool) hasVoted;
        mapping(address => bool) voteValue; // true = yes, false = no
    }
    
    struct ProposalView {
        uint256 proposalId;
        uint256 tokenId;
        address proposer;
        string title;
        string description;
        uint256 amount;
        ProposalType proposalType;
        ProposalStatus status;
        uint256 startTime;
        uint256 endTime;
        uint256 yesVotes;
        uint256 noVotes;
        bool executed;
    }
    
    // Minimum share percentage required to create a proposal (in basis points, e.g. 100 = 1%)
    uint256 public proposalThreshold = 100;
    
    // Percentage of yes votes required for a proposal to pass (in basis points, e.g. 5100 = 51%)
    uint256 public approvalThreshold = 5100;
    
    // Minimum voting period for proposals (in days)
    uint256 public minVotingPeriod = 3;
    
    // Maximum voting period for proposals (in days)
    uint256 public maxVotingPeriod = 14;
    
    Counters.Counter private _proposalIdCounter;
    
    // Mapping from proposal ID to Proposal
    mapping(uint256 => Proposal) public proposals;
    
    // Mapping from token ID to array of proposal IDs
    mapping(uint256 => uint256[]) public propertyProposals;
    
    // Events
    event ProposalCreated(uint256 indexed proposalId, uint256 indexed tokenId, address indexed proposer, string title, ProposalType proposalType, uint256 endTime);
    event VoteCast(uint256 indexed proposalId, address indexed voter, bool support, uint256 weight);
    event ProposalExecuted(uint256 indexed proposalId);
    event ProposalCancelled(uint256 indexed proposalId);
    event ThresholdUpdated(string thresholdType, uint256 newValue);
    
    constructor(address _propertyTokenAddress) {
        propertyToken = PropertyToken(_propertyTokenAddress);
        _proposalIdCounter.increment(); // Start from 1
    }
    
    /**
     * @dev Create a new proposal for a property
     * @param tokenId Property token ID
     * @param title Proposal title
     * @param description Detailed proposal description
     * @param amount Amount of funds required (if applicable)
     * @param proposalType Type of proposal
     * @param votingPeriodDays Voting period in days
     */
    function createProposal(
        uint256 tokenId,
        string memory title,
        string memory description,
        uint256 amount,
        ProposalType proposalType,
        uint256 votingPeriodDays
    ) external {
        require(bytes(title).length > 0, "Title cannot be empty");
        require(bytes(description).length > 0, "Description cannot be empty");
        require(votingPeriodDays >= minVotingPeriod && votingPeriodDays <= maxVotingPeriod, "Invalid voting period");
        
        // Get property details
        (uint256 totalShares,,,,) = propertyToken.getPropertyDetails(tokenId);
        require(totalShares > 0, "Invalid property");
        
        // Check if proposer has enough shares
        uint256 userShares = propertyToken.balanceOf(msg.sender, tokenId);
        require(userShares > 0, "No shares owned");
        
        uint256 userSharePercentage = (userShares * 10000) / totalShares;
        require(userSharePercentage >= proposalThreshold, "Not enough shares to create proposal");
        
        // Create proposal
        uint256 proposalId = _proposalIdCounter.current();
        _proposalIdCounter.increment();
        
        Proposal storage newProposal = proposals[proposalId];
        newProposal.proposalId = proposalId;
        newProposal.tokenId = tokenId;
        newProposal.proposer = msg.sender;
        newProposal.title = title;
        newProposal.description = description;
        newProposal.amount = amount;
        newProposal.proposalType = proposalType;
        newProposal.status = ProposalStatus.ACTIVE;
        newProposal.startTime = block.timestamp;
        newProposal.endTime = block.timestamp + (votingPeriodDays * 1 days);
        newProposal.yesVotes = 0;
        newProposal.noVotes = 0;
        newProposal.executed = false;
        
        // Add to property proposals
        propertyProposals[tokenId].push(proposalId);
        
        emit ProposalCreated(proposalId, tokenId, msg.sender, title, proposalType, newProposal.endTime);
    }
    
    /**
     * @dev Cast a vote on a proposal
     * @param proposalId Proposal ID
     * @param support Whether to support the proposal
     */
    function castVote(uint256 proposalId, bool support) external {
        Proposal storage proposal = proposals[proposalId];
        require(proposal.proposalId == proposalId, "Proposal does not exist");
        require(proposal.status == ProposalStatus.ACTIVE, "Proposal is not active");
        require(block.timestamp < proposal.endTime, "Voting period has ended");
        require(!proposal.hasVoted[msg.sender], "Already voted");
        
        // Get voter's shares
        uint256 userShares = propertyToken.balanceOf(msg.sender, proposal.tokenId);
        require(userShares > 0, "No shares owned");
        
        // Record vote
        proposal.hasVoted[msg.sender] = true;
        proposal.voteValue[msg.sender] = support;
        
        if (support) {
            proposal.yesVotes += userShares;
        } else {
            proposal.noVotes += userShares;
        }
        
        emit VoteCast(proposalId, msg.sender, support, userShares);
    }
    
    /**
     * @dev Execute a proposal after voting period ends
     * @param proposalId Proposal ID
     */
    function executeProposal(uint256 proposalId) external {
        Proposal storage proposal = proposals[proposalId];
        require(proposal.proposalId == proposalId, "Proposal does not exist");
        require(proposal.status == ProposalStatus.ACTIVE, "Proposal is not active");
        require(block.timestamp >= proposal.endTime, "Voting period not ended");
        require(!proposal.executed, "Proposal already executed");
        
        // Get property details
        (uint256 totalShares,,,,) = propertyToken.getPropertyDetails(proposal.tokenId);
        
        // Calculate results
        uint256 totalVotes = proposal.yesVotes + proposal.noVotes;
        uint256 participationRate = (totalVotes * 10000) / totalShares;
        uint256 approvalRate = (proposal.yesVotes * 10000) / totalVotes;
        
        // Minimum participation of 10%
        if (participationRate >= 1000 && approvalRate >= approvalThreshold) {
            proposal.status = ProposalStatus.APPROVED;
        } else {
            proposal.status = ProposalStatus.REJECTED;
        }
        
        proposal.executed = true;
        
        emit ProposalExecuted(proposalId);
    }
    
    /**
     * @dev Cancel a proposal (only proposer or admin can cancel)
     * @param proposalId Proposal ID
     */
    function cancelProposal(uint256 proposalId) external {
        Proposal storage proposal = proposals[proposalId];
        require(proposal.proposalId == proposalId, "Proposal does not exist");
        require(proposal.status == ProposalStatus.ACTIVE, "Proposal is not active");
        require(msg.sender == proposal.proposer || msg.sender == owner(), "Not authorized");
        
        proposal.status = ProposalStatus.CANCELLED;
        
        emit ProposalCancelled(proposalId);
    }
    
    /**
     * @dev Update the proposal threshold
     * @param newThreshold New threshold in basis points
     */
    function updateProposalThreshold(uint256 newThreshold) external onlyOwner {
        require(newThreshold > 0 && newThreshold <= 1000, "Invalid threshold value");
        proposalThreshold = newThreshold;
        emit ThresholdUpdated("ProposalThreshold", newThreshold);
    }
    
    /**
     * @dev Update the approval threshold
     * @param newThreshold New threshold in basis points
     */
    function updateApprovalThreshold(uint256 newThreshold) external onlyOwner {
        require(newThreshold >= 5000 && newThreshold <= 7500, "Invalid threshold value");
        approvalThreshold = newThreshold;
        emit ThresholdUpdated("ApprovalThreshold", newThreshold);
    }
    
    /**
     * @dev Update voting period constraints
     * @param newMinPeriod New minimum voting period in days.
     * @param newMaxPeriod New maximum voting period in days.
     */
    function updateVotingPeriods(uint256 newMinPeriod, uint256 newMaxPeriod) external onlyOwner {
        require(newMinPeriod > 0, "Minimum period must be positive");
        require(newMaxPeriod >= newMinPeriod, "Max period must be >= min period");
        require(newMaxPeriod <= 30, "Max period cannot exceed 30 days");
        
        minVotingPeriod = newMinPeriod;
        maxVotingPeriod = newMaxPeriod;
        
        emit ThresholdUpdated("MinVotingPeriod", newMinPeriod);
        emit ThresholdUpdated("MaxVotingPeriod", newMaxPeriod);
    }
    
    /**
     * @dev Get proposal details
     * @param proposalId Proposal ID
     */
    function getProposal(uint256 proposalId) external view returns (ProposalView memory) {
        Proposal storage proposal = proposals[proposalId];
        require(proposal.proposalId == proposalId, "Proposal does not exist");
        
        return ProposalView({
            proposalId: proposal.proposalId,
            tokenId: proposal.tokenId,
            proposer: proposal.proposer,
            title: proposal.title,
            description: proposal.description,
            amount: proposal.amount,
            proposalType: proposal.proposalType,
            status: proposal.status,
            startTime: proposal.startTime,
            endTime: proposal.endTime,
            yesVotes: proposal.yesVotes,
            noVotes: proposal.noVotes,
            executed: proposal.executed
        });
    }
    
    /**
     * @dev Get all proposals for a property 
     * @param tokenId Property token ID
     */
    function getPropertyProposalIds(uint256 tokenId) external view returns (uint256[] memory) {
        return propertyProposals[tokenId];
    }
    
    /**
     * @dev Check if a user has voted on a proposal - for UI display
     * @param proposalId Proposal ID
     * @param user User address
     */
    function hasVoted(uint256 proposalId, address user) external view returns (bool, bool) {
        Proposal storage proposal = proposals[proposalId];
        require(proposal.proposalId == proposalId, "Proposal does not exist");
        
        return (proposal.hasVoted[user], proposal.voteValue[user]);
    }
} 