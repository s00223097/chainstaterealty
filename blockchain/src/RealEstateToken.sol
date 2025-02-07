// SPDX-License-Identifier: MIT
pragma solidity ^0.8.19;

// OpenZepplin impkementations of the ERC1155 token standard  :- 
// GENERATE TEST TOKENS FIRS
import "@openzeppelin/contracts/token/ERC1155/ERC1155.sol";
import "@openzeppelin/contracts/access/Ownable.sol";

contract RealEstateToken is ERC1155, Ownable { // ERC1155
    struct Property {
        string propertyAddress;
        uint256 totalShares;
        uint256 pricePerShare;
        bool isActive;
    }
    
    mapping(uint256 => Property) public properties;
    uint256 private _propertyCounter;

    constructor() ERC1155("") Ownable(msg.sender) {}

    function createProperty(
        string memory propertyAddress,
        uint256 totalShares,
        uint256 pricePerShare
    ) public onlyOwner returns (uint256) {
        uint256 propertyId = _propertyCounter++;
        
        properties[propertyId] = Property({
            propertyAddress: propertyAddress,
            totalShares: totalShares,
            pricePerShare: pricePerShare,
            isActive: true
        });

        _mint(msg.sender, propertyId, totalShares, "");
        
        return propertyId;
    }

    function purchaseShares(uint256 propertyId, uint256 shares) public payable {
        Property storage property = properties[propertyId];
        require(property.isActive, "Property not active");
        require(shares > 0, "Must purchase at least one share");
        require(
            balanceOf(owner(), propertyId) >= shares,
            "There aren't enoug shares available on this property..."
        );
        require(
            msg.value >= shares * property.pricePerShare,
            "Insufficient payment made. Sorry it's more expesnive than you thought."
        );

        _safeTransferFrom(owner(), msg.sender, propertyId, shares, "");
    }
}
