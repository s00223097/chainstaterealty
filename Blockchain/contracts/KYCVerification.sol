// SPDX-License-Identifier: MIT
pragma solidity ^0.8.19;

import "@openzeppelin/contracts/access/AccessControl.sol";
import "@openzeppelin/contracts/security/Pausable.sol";

contract KYCVerification is AccessControl, Pausable {
    // Role definitions
    bytes32 public constant ADMIN_ROLE = keccak256("ADMIN_ROLE");
    bytes32 public constant VERIFIER_ROLE = keccak256("VERIFIER_ROLE");
    
    // KYC verification levels
    enum VerificationLevel {
        NONE,           // Not verified
        BASIC,          // Basic identity verification
        ACCREDITED,     // Accredited investor status
        INSTITUTIONAL   // Institutional investor
    }
    
    // Investor verification data
    struct InvestorData {
        address wallet;
        VerificationLevel level;
        uint256 verificationDate;
        uint256 expirationDate;
        string verificationHash; // Hash of off-chain verification documents
        bool isActive;
    }
    
    // Mappings
    mapping(address => InvestorData) public investors;
    mapping(address => bool) public blacklist;
    
    // Country restrictions (ISO country codes mapped to boolean)
    mapping(string => bool) public restrictedCountries;
    
    // Events
    event InvestorVerified(address indexed investor, VerificationLevel level, uint256 expirationDate);
    event InvestorRejected(address indexed investor, string reason);
    event InvestorBlacklisted(address indexed investor, string reason);
    event InvestorUnblacklisted(address indexed investor);
    event CountryRestrictionUpdated(string countryCode, bool isRestricted);
    event VerificationRevoked(address indexed investor, string reason);
    event VerificationRenewed(address indexed investor, uint256 newExpirationDate);
    
    constructor() {
        _grantRole(DEFAULT_ADMIN_ROLE, msg.sender);
        _grantRole(ADMIN_ROLE, msg.sender);
        _grantRole(VERIFIER_ROLE, msg.sender);
    }
    
    /**
     * @dev Verify an investor with KYC information
     * @param investor Investor wallet address
     * @param level Verification level
     * @param validityDays How long the verification is valid for in days
     * @param verificationHash Hash of off-chain verification documents
     */
    function verifyInvestor(
        address investor,
        VerificationLevel level,
        uint256 validityDays,
        string memory verificationHash
    ) external onlyRole(VERIFIER_ROLE) whenNotPaused {
        require(investor != address(0), "Invalid address");
        require(level != VerificationLevel.NONE, "Invalid verification level");
        require(validityDays > 0, "Validity must be greater than zero");
        require(bytes(verificationHash).length > 0, "Verification hash required");
        require(!blacklist[investor], "Investor is blacklisted");
        
        uint256 expirationDate = block.timestamp + (validityDays * 1 days);
        
        investors[investor] = InvestorData({
            wallet: investor,
            level: level,
            verificationDate: block.timestamp,
            expirationDate: expirationDate,
            verificationHash: verificationHash,
            isActive: true
        });
        
        emit InvestorVerified(investor, level, expirationDate);
    }
    
    /**
     * @dev Revoke verification for an investor
     * @param investor Investor wallet address
     * @param reason Reason for revocation
     */
    function revokeVerification(address investor, string memory reason) external onlyRole(VERIFIER_ROLE) {
        require(investor != address(0), "Invalid address");
        require(investors[investor].isActive, "Investor not active");
        
        investors[investor].isActive = false;
        
        emit VerificationRevoked(investor, reason);
    }
    
    /**
     * @dev Reject an investor's verification
     * @param investor Investor wallet address
     * @param reason Reason for rejection
     */
    function rejectInvestor(address investor, string memory reason) external onlyRole(VERIFIER_ROLE) {
        require(investor != address(0), "Invalid address");
        
        // Create a record with NONE level and inactive status
        investors[investor] = InvestorData({
            wallet: investor,
            level: VerificationLevel.NONE,
            verificationDate: block.timestamp,
            expirationDate: block.timestamp,
            verificationHash: "",
            isActive: false
        });
        
        emit InvestorRejected(investor, reason);
    }
    
    /**
     * @dev Add an investor to the blacklist
     * @param investor Investor wallet address
     * @param reason Reason for blacklisting
     */
    function blacklistInvestor(address investor, string memory reason) external onlyRole(ADMIN_ROLE) {
        require(investor != address(0), "Invalid address");
        
        blacklist[investor] = true;
        
        // If they have verification, revoke it
        if (investors[investor].isActive) {
            investors[investor].isActive = false;
        }
        
        emit InvestorBlacklisted(investor, reason);
    }
    
    /**
     * @dev Remove an investor from the blacklist
     * @param investor Investor wallet address
     */
    function unblacklistInvestor(address investor) external onlyRole(ADMIN_ROLE) {
        require(investor != address(0), "Invalid address");
        require(blacklist[investor], "Investor not blacklisted");
        
        blacklist[investor] = false;
        
        emit InvestorUnblacklisted(investor);
    }
    
    /**
     * @dev Renew an investor's verification
     * @param investor Investor wallet address
     * @param validityDays How long the verification is valid for in days
     */
    function renewVerification(address investor, uint256 validityDays) external onlyRole(VERIFIER_ROLE) whenNotPaused {
        require(investor != address(0), "Invalid address");
        require(investors[investor].isActive, "Investor not active");
        require(validityDays > 0, "Validity must be greater than zero");
        require(!blacklist[investor], "Investor is blacklisted");
        
        InvestorData storage data = investors[investor];
        uint256 newExpirationDate = block.timestamp + (validityDays * 1 days);
        data.expirationDate = newExpirationDate;
        
        emit VerificationRenewed(investor, newExpirationDate);
    }
    
    /**
     * @dev Update restricted country status
     * @param countryCode ISO country code
     * @param isRestricted Whether the country is restricted
     */
    function updateCountryRestriction(string memory countryCode, bool isRestricted) external onlyRole(ADMIN_ROLE) {
        require(bytes(countryCode).length > 0, "Country code required");
        
        restrictedCountries[countryCode] = isRestricted;
        
        emit CountryRestrictionUpdated(countryCode, isRestricted);
    }
    
    /**
     * @dev Check if an investor is verified at the required level
     * @param investor Investor wallet address
     * @param requiredLevel Minimum verification level required
     * @return bool Whether the investor meets the required verification level
     */
    function isVerified(address investor, VerificationLevel requiredLevel) external view returns (bool) {
        if (blacklist[investor]) {
            return false;
        }
        
        InvestorData storage data = investors[investor];
        
        if (!data.isActive) {
            return false;
        }
        
        if (block.timestamp > data.expirationDate) {
            return false;
        }
        
        return uint256(data.level) >= uint256(requiredLevel);
    }
    
    /**
     * @dev Get verification details for an investor
     * @param investor Investor wallet address
     */
    function getVerificationDetails(address investor) external view returns (
        VerificationLevel level,
        uint256 verificationDate,
        uint256 expirationDate,
        bool isActive,
        bool isBlacklisted
    ) {
        InvestorData storage data = investors[investor];
        
        return (
            data.level,
            data.verificationDate,
            data.expirationDate,
            data.isActive,
            blacklist[investor]
        );
    }
    
    /**
     * @dev Check if a verification has expired
     * @param investor Investor wallet address
     */
    function hasExpired(address investor) external view returns (bool) {
        return investors[investor].expirationDate < block.timestamp;
    }
    
    /**
     * @dev Check if a country is restricted
     * @param countryCode ISO country code
     */
    function isCountryRestricted(string memory countryCode) external view returns (bool) {
        return restrictedCountries[countryCode];
    }
    
    /**
     * @dev Pause all verification operations
     */
    function pause() external onlyRole(ADMIN_ROLE) {
        _pause();
    }
    
    /**
     * @dev Unpause all verification operations
     */
    function unpause() external onlyRole(ADMIN_ROLE) {
        _unpause();
    }
} 