const { expect } = require("chai");
const { ethers } = require("hardhat");

describe("PropertyMarketplace", function () {
  let PropertyToken;
  let PropertyMarketplace;
  let propertyToken;
  let marketplace;
  let owner;
  let addr1;
  let addr2;
  let addr3;

  beforeEach(async function () {
    [owner, addr1, addr2, addr3] = await ethers.getSigners();
    
    // Deploy PropertyToken
    PropertyToken = await ethers.getContractFactory("PropertyToken");
    propertyToken = await PropertyToken.deploy();
    await propertyToken.deployed();
    
    // Deploy PropertyMarketplace
    PropertyMarketplace = await ethers.getContractFactory("PropertyMarketplace");
    marketplace = await PropertyMarketplace.deploy(propertyToken.address);
    await marketplace.deployed();
    
    // Create a property and mint some shares
    await propertyToken.createProperty(1000, ethers.utils.parseEther("0.1"), "ipfs://test");
    await propertyToken.connect(addr1).purchaseShares(1, 100, { value: ethers.utils.parseEther("10") });
  });

  describe("Deployment", function () {
    it("Should set the right owner", async function () {
      expect(await marketplace.owner()).to.equal(owner.address);
    });

    it("Should set the right property token address", async function () {
      expect(await marketplace.propertyToken()).to.equal(propertyToken.address);
    });

    it("Should have the correct marketplace fee rate", async function () {
      expect(await marketplace.marketplaceFeeRate()).to.equal(250); // 2.5%
    });
  });

  describe("Listings", function () {
    beforeEach(async function () {
      // Approve marketplace to transfer tokens
      await propertyToken.connect(addr1).setApprovalForAll(marketplace.address, true);
    });

    it("Should create a new listing", async function () {
      const amount = 50;
      const pricePerShare = ethers.utils.parseEther("0.2");

      await expect(marketplace.connect(addr1).createListing(1, amount, pricePerShare))
        .to.emit(marketplace, "ListingCreated")
        .withArgs(1, addr1.address, 1, amount, pricePerShare);

      const listing = await marketplace.listings(1);
      expect(listing.seller).to.equal(addr1.address);
      expect(listing.tokenId).to.equal(1);
      expect(listing.amount).to.equal(amount);
      expect(listing.pricePerShare).to.equal(pricePerShare);
      expect(listing.isActive).to.equal(true);
    });

    it("Should fail to create listing with zero amount", async function () {
      await expect(marketplace.connect(addr1).createListing(1, 0, ethers.utils.parseEther("0.2")))
        .to.be.revertedWith("Amount must be greater than zero");
    });

    it("Should fail to create listing with zero price", async function () {
      await expect(marketplace.connect(addr1).createListing(1, 50, 0))
        .to.be.revertedWith("Price must be greater than zero");
    });

    it("Should fail to create listing with insufficient shares", async function () {
      await expect(marketplace.connect(addr1).createListing(1, 200, ethers.utils.parseEther("0.2")))
        .to.be.revertedWith("Insufficient shares owned");
    });

    it("Should update a listing", async function () {
      await marketplace.connect(addr1).createListing(1, 50, ethers.utils.parseEther("0.2"));
      
      const newAmount = 30;
      const newPrice = ethers.utils.parseEther("0.3");
      
      await expect(marketplace.connect(addr1).updateListing(1, newAmount, newPrice))
        .to.emit(marketplace, "ListingUpdated")
        .withArgs(1, newAmount, newPrice);

      const listing = await marketplace.listings(1);
      expect(listing.amount).to.equal(newAmount);
      expect(listing.pricePerShare).to.equal(newPrice);
    });

    it("Should cancel a listing", async function () {
      await marketplace.connect(addr1).createListing(1, 50, ethers.utils.parseEther("0.2"));
      
      await expect(marketplace.connect(addr1).cancelListing(1))
        .to.emit(marketplace, "ListingCancelled")
        .withArgs(1);

      const listing = await marketplace.listings(1);
      expect(listing.isActive).to.equal(false);
      expect(await propertyToken.balanceOf(addr1.address, 1)).to.equal(100);
    });

    it("Should allow owner to cancel any listing", async function () {
      await marketplace.connect(addr1).createListing(1, 50, ethers.utils.parseEther("0.2"));
      
      await expect(marketplace.cancelListing(1))
        .to.emit(marketplace, "ListingCancelled")
        .withArgs(1);

      const listing = await marketplace.listings(1);
      expect(listing.isActive).to.equal(false);
    });
  });

  describe("Purchasing", function () {
    beforeEach(async function () {
      // Approve marketplace to transfer tokens
      await propertyToken.connect(addr1).setApprovalForAll(marketplace.address, true);
      await marketplace.connect(addr1).createListing(1, 50, ethers.utils.parseEther("0.2"));
    });

    it("Should purchase shares from a listing", async function () {
      const amount = 30;
      const totalPrice = amount * ethers.utils.parseEther("0.2");
      const marketplaceFee = (totalPrice * 250) / 10000;
      const sellerPayment = totalPrice - marketplaceFee;

      const initialSellerBalance = await addr1.getBalance();
      const initialContractBalance = await ethers.provider.getBalance(marketplace.address);

      await expect(marketplace.connect(addr2).purchaseShares(1, amount, { value: totalPrice }))
        .to.emit(marketplace, "SharesPurchased")
        .withArgs(1, addr2.address, amount, totalPrice);

      const listing = await marketplace.listings(1);
      expect(listing.amount).to.equal(20);
      expect(await propertyToken.balanceOf(addr2.address, 1)).to.equal(amount);
      
      const finalSellerBalance = await addr1.getBalance();
      const finalContractBalance = await ethers.provider.getBalance(marketplace.address);
      
      expect(finalSellerBalance.sub(initialSellerBalance)).to.equal(sellerPayment);
      expect(finalContractBalance.sub(initialContractBalance)).to.equal(marketplaceFee);
    });

    it("Should fail to purchase from inactive listing", async function () {
      await marketplace.connect(addr1).cancelListing(1);
      
      await expect(marketplace.connect(addr2).purchaseShares(1, 30, { value: ethers.utils.parseEther("6") }))
        .to.be.revertedWith("Listing is not active");
    });

    it("Should fail to purchase with invalid amount", async function () {
      await expect(marketplace.connect(addr2).purchaseShares(1, 0, { value: ethers.utils.parseEther("6") }))
        .to.be.revertedWith("Invalid amount");
      
      await expect(marketplace.connect(addr2).purchaseShares(1, 60, { value: ethers.utils.parseEther("12") }))
        .to.be.revertedWith("Invalid amount");
    });

    it("Should fail to purchase with insufficient payment", async function () {
      await expect(marketplace.connect(addr2).purchaseShares(1, 30, { value: ethers.utils.parseEther("5") }))
        .to.be.revertedWith("Insufficient payment");
    });
  });

  describe("Auctions", function () {
    beforeEach(async function () {
      // Approve marketplace to transfer tokens
      await propertyToken.connect(addr1).setApprovalForAll(marketplace.address, true);
    });

    it("Should create a new auction", async function () {
      const amount = 50;
      const startingPrice = ethers.utils.parseEther("1");
      const durationHours = 24;

      await expect(marketplace.connect(addr1).createAuction(1, amount, startingPrice, durationHours))
        .to.emit(marketplace, "AuctionCreated")
        .withArgs(1, addr1.address, 1, amount, startingPrice, (await ethers.provider.getBlock("latest")).timestamp + (durationHours * 3600));

      const auction = await marketplace.auctions(1);
      expect(auction.seller).to.equal(addr1.address);
      expect(auction.tokenId).to.equal(1);
      expect(auction.amount).to.equal(amount);
      expect(auction.startingPrice).to.equal(startingPrice);
      expect(auction.currentBid).to.equal(0);
      expect(auction.currentBidder).to.equal(ethers.constants.AddressZero);
      expect(auction.isActive).to.equal(true);
      expect(auction.isClaimed).to.equal(false);
    });

    it("Should place a bid on an auction", async function () {
      await marketplace.connect(addr1).createAuction(1, 50, ethers.utils.parseEther("1"), 24);
      
      const bidAmount = ethers.utils.parseEther("1.5");
      
      await expect(marketplace.connect(addr2).placeBid(1, { value: bidAmount }))
        .to.emit(marketplace, "BidPlaced")
        .withArgs(1, addr2.address, bidAmount);

      const auction = await marketplace.auctions(1);
      expect(auction.currentBid).to.equal(bidAmount);
      expect(auction.currentBidder).to.equal(addr2.address);
    });

    it("Should end an auction and distribute assets", async function () {
      await marketplace.connect(addr1).createAuction(1, 50, ethers.utils.parseEther("1"), 1);
      await marketplace.connect(addr2).placeBid(1, { value: ethers.utils.parseEther("1.5") });
      
      // Fast forward time
      await ethers.provider.send("evm_increaseTime", [3600]);
      await ethers.provider.send("evm_mine");
      
      const marketplaceFee = (ethers.utils.parseEther("1.5") * 250) / 10000;
      const sellerPayment = ethers.utils.parseEther("1.5").sub(marketplaceFee);
      
      const initialSellerBalance = await addr1.getBalance();
      const initialContractBalance = await ethers.provider.getBalance(marketplace.address);
      
      await expect(marketplace.connect(addr3).endAuction(1))
        .to.emit(marketplace, "AuctionEnded")
        .withArgs(1, addr2.address, ethers.utils.parseEther("1.5"));

      const auction = await marketplace.auctions(1);
      expect(auction.isActive).to.equal(false);
      expect(await propertyToken.balanceOf(addr2.address, 1)).to.equal(50);
      
      const finalSellerBalance = await addr1.getBalance();
      const finalContractBalance = await ethers.provider.getBalance(marketplace.address);
      
      expect(finalSellerBalance.sub(initialSellerBalance)).to.equal(sellerPayment);
      expect(finalContractBalance.sub(initialContractBalance)).to.equal(marketplaceFee);
    });

    it("Should fail to place bid below minimum", async function () {
      await marketplace.connect(addr1).createAuction(1, 50, ethers.utils.parseEther("1"), 24);
      
      await expect(marketplace.connect(addr2).placeBid(1, { value: ethers.utils.parseEther("0.5") }))
        .to.be.revertedWith("Bid too low");
    });

    it("Should fail to place bid on ended auction", async function () {
      await marketplace.connect(addr1).createAuction(1, 50, ethers.utils.parseEther("1"), 1);
      
      // Fast forward time
      await ethers.provider.send("evm_increaseTime", [3600]);
      await ethers.provider.send("evm_mine");
      
      await expect(marketplace.connect(addr2).placeBid(1, { value: ethers.utils.parseEther("1.5") }))
        .to.be.revertedWith("Auction has ended");
    });
  });
}); 