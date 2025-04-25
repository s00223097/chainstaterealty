const { expect } = require("chai");
const { ethers } = require("hardhat");

describe("PropertyToken", function () {
  let PropertyToken;
  let propertyToken;
  let owner;
  let addr1;
  let addr2;
  let addr3;

  beforeEach(async function () {
    [owner, addr1, addr2, addr3] = await ethers.getSigners();
    PropertyToken = await ethers.getContractFactory("PropertyToken");
    propertyToken = await PropertyToken.deploy();
    await propertyToken.deployed();
  });

  describe("Deployment", function () {
    it("Should set the right owner", async function () {
      expect(await propertyToken.owner()).to.equal(owner.address);
    });
  });

  describe("Property Creation", function () {
    it("Should create a new property with correct parameters", async function () {
      const totalShares = 1000;
      const pricePerShare = ethers.utils.parseEther("0.1");
      const propertyURI = "ipfs://test";

      await expect(propertyToken.createProperty(totalShares, pricePerShare, propertyURI))
        .to.emit(propertyToken, "PropertyCreated")
        .withArgs(1, totalShares, pricePerShare, propertyURI);

      const property = await propertyToken.properties(1);
      expect(property.tokenId).to.equal(1);
      expect(property.totalShares).to.equal(totalShares);
      expect(property.availableShares).to.equal(totalShares);
      expect(property.pricePerShare).to.equal(pricePerShare);
      expect(property.propertyURI).to.equal(propertyURI);
      expect(property.active).to.equal(true);
    });

    it("Should fail to create property with zero shares", async function () {
      await expect(propertyToken.createProperty(0, 100, "ipfs://test"))
        .to.be.revertedWith("Shares must be greater than zero");
    });

    it("Should fail to create property with zero price", async function () {
      await expect(propertyToken.createProperty(100, 0, "ipfs://test"))
        .to.be.revertedWith("Price must be greater than zero");
    });

    it("Should fail to create property with empty URI", async function () {
      await expect(propertyToken.createProperty(100, 100, ""))
        .to.be.revertedWith("URI cannot be empty");
    });

    it("Should fail if non-owner tries to create property", async function () {
      await expect(propertyToken.connect(addr1).createProperty(100, 100, "ipfs://test"))
        .to.be.revertedWith("Ownable: caller is not the owner");
    });
  });

  describe("Share Purchasing", function () {
    beforeEach(async function () {
      await propertyToken.createProperty(1000, ethers.utils.parseEther("0.1"), "ipfs://test");
    });

    it("Should allow users to purchase shares", async function () {
      const amount = 100;
      const cost = amount * ethers.utils.parseEther("0.1");

      await expect(propertyToken.connect(addr1).purchaseShares(1, amount, { value: cost }))
        .to.emit(propertyToken, "SharesPurchased")
        .withArgs(addr1.address, 1, amount, cost);

      expect(await propertyToken.balanceOf(addr1.address, 1)).to.equal(amount);
      expect(await propertyToken.properties(1)).to.have.property("availableShares", 900);
    });

    it("Should return excess payment", async function () {
      const amount = 100;
      const cost = amount * ethers.utils.parseEther("0.1");
      const excessPayment = ethers.utils.parseEther("0.5");

      const initialBalance = await addr1.getBalance();
      const tx = await propertyToken.connect(addr1).purchaseShares(1, amount, { value: cost.add(excessPayment) });
      const receipt = await tx.wait();
      const gasUsed = receipt.gasUsed.mul(tx.gasPrice);
      const finalBalance = await addr1.getBalance();

      expect(finalBalance).to.equal(initialBalance.sub(cost).sub(gasUsed));
    });

    it("Should fail if property is not active", async function () {
      await propertyToken.setPropertyActive(1, false);
      await expect(propertyToken.connect(addr1).purchaseShares(1, 100, { value: ethers.utils.parseEther("10") }))
        .to.be.revertedWith("Property is not active");
    });

    it("Should fail if not enough shares available", async function () {
      await expect(propertyToken.connect(addr1).purchaseShares(1, 2000, { value: ethers.utils.parseEther("200") }))
        .to.be.revertedWith("Not enough shares available");
    });

    it("Should fail if insufficient payment", async function () {
      await expect(propertyToken.connect(addr1).purchaseShares(1, 100, { value: ethers.utils.parseEther("5") }))
        .to.be.revertedWith("Insufficient payment");
    });
  });

  describe("Share Selling", function () {
    beforeEach(async function () {
      await propertyToken.createProperty(1000, ethers.utils.parseEther("0.1"), "ipfs://test");
      await propertyToken.connect(addr1).purchaseShares(1, 100, { value: ethers.utils.parseEther("10") });
    });

    it("Should allow users to sell shares", async function () {
      const amount = 50;
      const payment = amount * ethers.utils.parseEther("0.1");

      const initialBalance = await addr1.getBalance();
      const tx = await propertyToken.connect(addr1).sellShares(1, amount);
      const receipt = await tx.wait();
      const gasUsed = receipt.gasUsed.mul(tx.gasPrice);
      const finalBalance = await addr1.getBalance();

      expect(finalBalance).to.equal(initialBalance.add(payment).sub(gasUsed));
      expect(await propertyToken.balanceOf(addr1.address, 1)).to.equal(50);
      expect(await propertyToken.properties(1)).to.have.property("availableShares", 950);
    });

    it("Should fail if property is not active", async function () {
      await propertyToken.setPropertyActive(1, false);
      await expect(propertyToken.connect(addr1).sellShares(1, 50))
        .to.be.revertedWith("Property is not active");
    });

    it("Should fail if not enough shares owned", async function () {
      await expect(propertyToken.connect(addr1).sellShares(1, 200))
        .to.be.revertedWith("Not enough shares owned");
    });
  });

  describe("Property Management", function () {
    beforeEach(async function () {
      await propertyToken.createProperty(1000, ethers.utils.parseEther("0.1"), "ipfs://test");
    });

    it("Should allow owner to update property price", async function () {
      const newPrice = ethers.utils.parseEther("0.2");
      await propertyToken.updatePropertyPrice(1, newPrice);
      expect(await propertyToken.properties(1)).to.have.property("pricePerShare", newPrice);
    });

    it("Should allow owner to update property URI", async function () {
      const newURI = "ipfs://new";
      await propertyToken.setPropertyURI(1, newURI);
      expect(await propertyToken.properties(1)).to.have.property("propertyURI", newURI);
    });

    it("Should allow owner to toggle property active status", async function () {
      await propertyToken.setPropertyActive(1, false);
      expect(await propertyToken.properties(1)).to.have.property("active", false);
      
      await propertyToken.setPropertyActive(1, true);
      expect(await propertyToken.properties(1)).to.have.property("active", true);
    });

    it("Should fail if non-owner tries to update property", async function () {
      await expect(propertyToken.connect(addr1).updatePropertyPrice(1, ethers.utils.parseEther("0.2")))
        .to.be.revertedWith("Ownable: caller is not the owner");
      
      await expect(propertyToken.connect(addr1).setPropertyURI(1, "ipfs://new"))
        .to.be.revertedWith("Ownable: caller is not the owner");
      
      await expect(propertyToken.connect(addr1).setPropertyActive(1, false))
        .to.be.revertedWith("Ownable: caller is not the owner");
    });
  });

  describe("Property Details", function () {
    beforeEach(async function () {
      await propertyToken.createProperty(1000, ethers.utils.parseEther("0.1"), "ipfs://test");
    });

    it("Should return correct property details", async function () {
      const [totalShares, availableShares, pricePerShare, propertyURI, active] = 
        await propertyToken.getPropertyDetails(1);
      
      expect(totalShares).to.equal(1000);
      expect(availableShares).to.equal(1000);
      expect(pricePerShare).to.equal(ethers.utils.parseEther("0.1"));
      expect(propertyURI).to.equal("ipfs://test");
      expect(active).to.equal(true);
    });

    it("Should return correct owned properties", async function () {
      await propertyToken.connect(addr1).purchaseShares(1, 100, { value: ethers.utils.parseEther("10") });
      
      const [tokenIds, amounts] = await propertyToken.getOwnedProperties(addr1.address);
      
      expect(tokenIds.length).to.equal(1);
      expect(amounts.length).to.equal(1);
      expect(tokenIds[0]).to.equal(1);
      expect(amounts[0]).to.equal(100);
    });
  });

  describe("Withdrawal", function () {
    beforeEach(async function () {
      await propertyToken.createProperty(1000, ethers.utils.parseEther("0.1"), "ipfs://test");
      await propertyToken.connect(addr1).purchaseShares(1, 100, { value: ethers.utils.parseEther("10") });
    });

    it("Should allow owner to withdraw contract balance", async function () {
      const initialBalance = await owner.getBalance();
      const contractBalance = await ethers.provider.getBalance(propertyToken.address);
      
      const tx = await propertyToken.withdraw();
      const receipt = await tx.wait();
      const gasUsed = receipt.gasUsed.mul(tx.gasPrice);
      const finalBalance = await owner.getBalance();
      
      expect(finalBalance).to.equal(initialBalance.add(contractBalance).sub(gasUsed));
      expect(await ethers.provider.getBalance(propertyToken.address)).to.equal(0);
    });

    it("Should fail if non-owner tries to withdraw", async function () {
      await expect(propertyToken.connect(addr1).withdraw())
        .to.be.revertedWith("Ownable: caller is not the owner");
    });

    it("Should fail if contract has no balance", async function () {
      await propertyToken.withdraw();
      await expect(propertyToken.withdraw())
        .to.be.revertedWith("No balance to withdraw");
    });
  });
}); 