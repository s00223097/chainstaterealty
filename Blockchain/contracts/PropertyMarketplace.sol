// SPDX-License-Identifier: MIT
pragma solidity ^0.8.19;

import "@openzeppelin/contracts/access/Ownable.sol";
import "@openzeppelin/contracts/security/ReentrancyGuard.sol";
import "@openzeppelin/contracts/token/ERC1155/utils/ERC1155Holder.sol";
import "@openzeppelin/contracts/token/ERC1155/IERC1155.sol";
import "./PropertyToken.sol";

contract PropertyMarketplace is Ownable, ReentrancyGuard, ERC1155Holder {
    PropertyToken public propertyToken;
    
    // Marketplace fee percentage (in basis points, e.g., 250 = 2.5%)
    uint256 public marketplaceFeeRate = 250; 
    
    // Listing structure
    struct Listing {
        address seller;
        uint256 tokenId;
        uint256 amount;
        uint256 pricePerShare;
        bool isActive;
    }
    
    // Auction structure
    struct Auction {
        address seller;
        uint256 tokenId;
        uint256 amount;
        uint256 startingPrice;
        uint256 currentBid;
        address currentBidder;
        uint256 endTime;
        bool isActive;
        bool isClaimed;
    }
    
    // Mappings
    mapping(uint256 => Listing) public listings;
    mapping(uint256 => Auction) public auctions;
    uint256 public nextListingId = 1;
    uint256 public nextAuctionId = 1;
    
    // Events
    event ListingCreated(uint256 indexed listingId, address indexed seller, uint256 indexed tokenId, uint256 amount, uint256 pricePerShare);
    event ListingUpdated(uint256 indexed listingId, uint256 newAmount, uint256 newPricePerShare);
    event ListingCancelled(uint256 indexed listingId);
    event SharesPurchased(uint256 indexed listingId, address indexed buyer, uint256 amount, uint256 totalPrice);
    
    event AuctionCreated(uint256 indexed auctionId, address indexed seller, uint256 indexed tokenId, uint256 amount, uint256 startingPrice, uint256 endTime);
    event BidPlaced(uint256 indexed auctionId, address indexed bidder, uint256 bidAmount);
    event AuctionEnded(uint256 indexed auctionId, address indexed winner, uint256 winningBid);
    event AuctionCancelled(uint256 indexed auctionId);
    
    constructor(address _propertyTokenAddress) {
        propertyToken = PropertyToken(_propertyTokenAddress);
    }
    
    /**
     * @dev Create a new listing for property shares
     * @param tokenId The property token ID
     * @param amount Amount of shares to list
     * @param pricePerShare Price per share in wei
     */
    function createListing(uint256 tokenId, uint256 amount, uint256 pricePerShare) external nonReentrant {
        require(amount > 0, "Amount must be greater than zero");
        require(pricePerShare > 0, "Price must be greater than zero");
        require(propertyToken.balanceOf(msg.sender, tokenId) >= amount, "Insufficient shares owned");
        
        // Create new listing - TODO: Add propertyURI
        uint256 listingId = nextListingId++;
        listings[listingId] = Listing({
            seller: msg.sender,
            tokenId: tokenId,
            amount: amount,
            pricePerShare: pricePerShare,
            isActive: true
        });
        
        // Transfer shares to marketplace contract 
        propertyToken.safeTransferFrom(msg.sender, address(this), tokenId, amount, "");
        
        emit ListingCreated(listingId, msg.sender, tokenId, amount, pricePerShare);
    }
    
    /**
     * @dev Update an existing listing
     * @param listingId The listing ID
     * @param newAmount New amount of shares (0 to keep current)
     * @param newPricePerShare New price per share (0 to keep current)
     */
    function updateListing(uint256 listingId, uint256 newAmount, uint256 newPricePerShare) external nonReentrant {
        Listing storage listing = listings[listingId];
        require(listing.isActive, "Listing is not active");
        require(msg.sender == listing.seller, "Not the seller");
        
        if (newAmount > 0 && newAmount != listing.amount) {
            if (newAmount > listing.amount) {
                // Transfer additional shares to contract
                uint256 additionalAmount = newAmount - listing.amount;
                propertyToken.safeTransferFrom(msg.sender, address(this), listing.tokenId, additionalAmount, "");
            } else {
                // Return excess shares to seller
                uint256 excessAmount = listing.amount - newAmount;
                propertyToken.safeTransferFrom(address(this), msg.sender, listing.tokenId, excessAmount, "");
            }
            listing.amount = newAmount;
        }
        
        if (newPricePerShare > 0) {
            listing.pricePerShare = newPricePerShare;
        }
        
        emit ListingUpdated(listingId, listing.amount, listing.pricePerShare);
    }
    
    /**
     * @dev Cancel a listing and return shares to seller
     * @param listingId The listing ID
     */
    function cancelListing(uint256 listingId) external nonReentrant {
        Listing storage listing = listings[listingId];
        require(listing.isActive, "Listing is not active");
        require(msg.sender == listing.seller || msg.sender == owner(), "Not authorized");
        
        listing.isActive = false;
        
        // Return shares to seller
        propertyToken.safeTransferFrom(address(this), listing.seller, listing.tokenId, listing.amount, "");
        
        emit ListingCancelled(listingId);
    }
    
    /**
     * @dev Purchase shares from a listing
     * @param listingId The listing ID
     * @param amount Amount of shares to purchase
     */
    function purchaseShares(uint256 listingId, uint256 amount) external payable nonReentrant {
        Listing storage listing = listings[listingId];
        require(listing.isActive, "Listing is not active");
        require(amount > 0 && amount <= listing.amount, "Invalid amount");
        
        uint256 totalPrice = amount * listing.pricePerShare;
        require(msg.value >= totalPrice, "Insufficient payment");
        
        // Calculate marketplace fee
        uint256 marketplaceFee = (totalPrice * marketplaceFeeRate) / 10000;
        uint256 sellerPayment = totalPrice - marketplaceFee;
        
        // Update listing
        listing.amount -= amount;
        if (listing.amount == 0) {
            listing.isActive = false;
        }
        
        // Transfer shares to buyer
        propertyToken.safeTransferFrom(address(this), msg.sender, listing.tokenId, amount, "");
        
        // Pay seller
        payable(listing.seller).transfer(sellerPayment);
        
        // Return excess payment if any
        if (msg.value > totalPrice) {
            payable(msg.sender).transfer(msg.value - totalPrice);
        }
        
        emit SharesPurchased(listingId, msg.sender, amount, totalPrice);
    }
    
    /**
     * @dev Create a new auction for property shares
     * @param tokenId The property token ID
     * @param amount Amount of shares to auction
     * @param startingPrice Starting price in wei
     * @param durationHours Auction duration in hours
     */
    function createAuction(uint256 tokenId, uint256 amount, uint256 startingPrice, uint256 durationHours) external nonReentrant {
        require(amount > 0, "Amount must be greater than zero");
        require(startingPrice > 0, "Starting price must be greater than zero");
        require(durationHours > 0 && durationHours <= 168, "Duration must be between 1 hour and 7 days");
        require(propertyToken.balanceOf(msg.sender, tokenId) >= amount, "Insufficient shares owned");
        
        uint256 endTime = block.timestamp + (durationHours * 1 hours);
        
        // Create new auction
        uint256 auctionId = nextAuctionId++;
        auctions[auctionId] = Auction({
            seller: msg.sender,
            tokenId: tokenId,
            amount: amount,
            startingPrice: startingPrice,
            currentBid: 0,
            currentBidder: address(0),
            endTime: endTime,
            isActive: true,
            isClaimed: false
        });
        
        // Transfer shares to marketplace contract
        propertyToken.safeTransferFrom(msg.sender, address(this), tokenId, amount, "");
        
        emit AuctionCreated(auctionId, msg.sender, tokenId, amount, startingPrice, endTime);
    }
    
    /**
     * @dev Place a bid on an auction
     * @param auctionId The auction ID
     */
    function placeBid(uint256 auctionId) external payable nonReentrant {
        Auction storage auction = auctions[auctionId];
        require(auction.isActive, "Auction is not active");
        require(block.timestamp < auction.endTime, "Auction has ended");
        
        uint256 minBid = auction.currentBid > 0 ? auction.currentBid + (auction.currentBid / 10) : auction.startingPrice;
        require(msg.value >= minBid, "Bid too low");
        
        // Return previous bid to previous bidder
        if (auction.currentBid > 0) {
            payable(auction.currentBidder).transfer(auction.currentBid);
        }
        
        // Update auction with new bid
        auction.currentBid = msg.value;
        auction.currentBidder = msg.sender;
        
        emit BidPlaced(auctionId, msg.sender, msg.value);
    }
    
    /**
     * @dev End an auction and distribute assets
     * @param auctionId The auction ID
     */
    function endAuction(uint256 auctionId) external nonReentrant {
        Auction storage auction = auctions[auctionId];
        require(auction.isActive, "Auction is not active");
        require(block.timestamp >= auction.endTime || msg.sender == owner(), "Auction still active");
        
        auction.isActive = false;
        
        if (auction.currentBidder != address(0)) {
            // Auction had at least one bid
            
            // Calculate marketplace fee
            uint256 marketplaceFee = (auction.currentBid * marketplaceFeeRate) / 10000;
            uint256 sellerPayment = auction.currentBid - marketplaceFee;
            
            // Transfer payment to seller
            payable(auction.seller).transfer(sellerPayment);
            
            // Mark as ready for claiming
            auction.isClaimed = false;
            
            emit AuctionEnded(auctionId, auction.currentBidder, auction.currentBid);
        } else {
            // No bids, return shares to seller
            propertyToken.safeTransferFrom(address(this), auction.seller, auction.tokenId, auction.amount, "");
            auction.isClaimed = true;
            
            emit AuctionCancelled(auctionId);
        }
    }
    
    /**
     * @dev Claim shares won in auction
     * @param auctionId The auction ID
     */
    function claimAuctionShares(uint256 auctionId) external nonReentrant {
        Auction storage auction = auctions[auctionId];
        require(!auction.isActive, "Auction is still active");
        require(!auction.isClaimed, "Shares already claimed");
        require(msg.sender == auction.currentBidder, "Not the auction winner");
        
        auction.isClaimed = true;
        
        // Transfer shares to winner
        propertyToken.safeTransferFrom(address(this), auction.currentBidder, auction.tokenId, auction.amount, "");
    }
    
    /**
     * @dev Cancel an auction and return shares
     * @param auctionId The auction ID
     */
    function cancelAuction(uint256 auctionId) external nonReentrant {
        Auction storage auction = auctions[auctionId];
        require(auction.isActive, "Auction is not active");
        require(msg.sender == auction.seller || msg.sender == owner(), "Not authorized");
        require(auction.currentBidder == address(0), "Cannot cancel auction with bids");
        
        auction.isActive = false;
        auction.isClaimed = true;
        
        // Return shares to seller
        propertyToken.safeTransferFrom(address(this), auction.seller, auction.tokenId, auction.amount, "");
        
        emit AuctionCancelled(auctionId);
    }
    
    /**
     * @dev Update marketplace fee rate (owner only)
     * @param newFeeRate New fee rate in basis points (e.g., 250 = 2.5%)
     */
    function updateMarketplaceFeeRate(uint256 newFeeRate) external onlyOwner {
        require(newFeeRate <= 1000, "Fee cannot exceed 10%");
        marketplaceFeeRate = newFeeRate;
    }
    
    /**
     * @dev Withdraw accumulated marketplace fees (owner only)
     */
    function withdrawFees() external onlyOwner {
        uint256 balance = address(this).balance;
        require(balance > 0, "No balance to withdraw");
        payable(owner()).transfer(balance);
    }
} 