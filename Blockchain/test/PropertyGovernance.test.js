const { expect } = require("chai");
const { ethers } = require("hardhat");

describe("PropertyGovernance", function () {
  let PropertyToken;
  let PropertyGovernance;
  let propertyToken;
  let governance;
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
    
    // Deploy PropertyGovernance
    PropertyGovernance = await ethers.getContractFactory("PropertyGovernance");
    governance = await PropertyGovernance.deploy(propertyToken.address);
    await governance.deployed();
    
    // Create a property and mint some shares
    await propertyToken.createProperty(1000, ethers.utils.parseEther("0.1"), "ipfs://test");
    await propertyToken.connect(addr1).purchaseShares(1, 100, { value: ethers.utils.parseEther("10") });
    await propertyToken.connect(addr2).purchaseShares(1, 200, { value: ethers.utils.parseEther("20") });
  });

  describe("Deployment", function () {
    it("Should set the right owner", async function () {
      expect(await governance.owner()).to.equal(owner.address);
    });

    it("Should set the right property token address", async function () {
      expect(await governance.propertyToken()).to.equal(propertyToken.address);
    });

    it("Should have the correct default thresholds", async function () {
      expect(await governance.proposalThreshold()).to.equal(100); // 1%
      expect(await governance.approvalThreshold()).to.equal(5100); // 51%
    });

    it("Should have the correct voting period constraints", async function () {
      expect(await governance.minVotingPeriod()).to.equal(3);
      expect(await governance.maxVotingPeriod()).to.equal(14);
    });
  });

  describe("Proposal Creation", function () {
    it("Should create a new proposal", async function () {
      const title = "Maintenance Proposal";
      const description = "Fix the roof";
      const amount = ethers.utils.parseEther("1");
      const proposalType = 0; // MAINTENANCE
      const votingPeriodDays = 7;

      await expect(governance.connect(addr1).createProposal(1, title, description, amount, proposalType, votingPeriodDays))
        .to.emit(governance, "ProposalCreated")
        .withArgs(1, 1, addr1.address, title, proposalType, (await ethers.provider.getBlock("latest")).timestamp + (votingPeriodDays * 86400));

      const proposal = await governance.proposals(1);
      expect(proposal.proposalId).to.equal(1);
      expect(proposal.tokenId).to.equal(1);
      expect(proposal.proposer).to.equal(addr1.address);
      expect(proposal.title).to.equal(title);
      expect(proposal.description).to.equal(description);
      expect(proposal.amount).to.equal(amount);
      expect(proposal.proposalType).to.equal(proposalType);
      expect(proposal.status).to.equal(0); // ACTIVE
      expect(proposal.executed).to.equal(false);
    });

    it("Should fail to create proposal with invalid parameters", async function () {
      await expect(governance.connect(addr1).createProposal(1, "", "Description", 0, 0, 7))
        .to.be.revertedWith("Title cannot be empty");
      
      await expect(governance.connect(addr1).createProposal(1, "Title", "", 0, 0, 7))
        .to.be.revertedWith("Description cannot be empty");
      
      await expect(governance.connect(addr1).createProposal(1, "Title", "Description", 0, 0, 2))
        .to.be.revertedWith("Invalid voting period");
      
      await expect(governance.connect(addr1).createProposal(1, "Title", "Description", 0, 0, 15))
        .to.be.revertedWith("Invalid voting period");
    });

    it("Should fail to create proposal with insufficient shares", async function () {
      await expect(governance.connect(addr3).createProposal(1, "Title", "Description", 0, 0, 7))
        .to.be.revertedWith("No shares owned");
      
      // Buy just 1 share (0.1%)
      await propertyToken.connect(addr3).purchaseShares(1, 1, { value: ethers.utils.parseEther("0.1") });
      
      await expect(governance.connect(addr3).createProposal(1, "Title", "Description", 0, 0, 7))
        .to.be.revertedWith("Not enough shares to create proposal");
    });
  });

  describe("Voting", function () {
    beforeEach(async function () {
      await governance.connect(addr1).createProposal(1, "Title", "Description", 0, 0, 7);
    });

    it("Should cast a vote", async function () {
      await expect(governance.connect(addr1).castVote(1, true))
        .to.emit(governance, "VoteCast")
        .withArgs(1, addr1.address, true, 100);

      const proposal = await governance.proposals(1);
      expect(proposal.yesVotes).to.equal(100);
      expect(proposal.noVotes).to.equal(0);
      expect(proposal.hasVoted[addr1.address]).to.equal(true);
      expect(proposal.voteValue[addr1.address]).to.equal(true);
    });

    it("Should fail to vote with invalid conditions", async function () {
      await expect(governance.connect(addr3).castVote(1, true))
        .to.be.revertedWith("No shares owned");
      
      await governance.connect(addr1).castVote(1, true);
      
      await expect(governance.connect(addr1).castVote(1, true))
        .to.be.revertedWith("Already voted");
      
      // Fast forward time
      await ethers.provider.send("evm_increaseTime", [8 * 86400]);
      await ethers.provider.send("evm_mine");
      
      await expect(governance.connect(addr2).castVote(1, true))
        .to.be.revertedWith("Voting period has ended");
    });
  });

  describe("Proposal Execution", function () {
    beforeEach(async function () {
      await governance.connect(addr1).createProposal(1, "Title", "Description", 0, 0, 7);
      await governance.connect(addr1).castVote(1, true);
      await governance.connect(addr2).castVote(1, true);
    });

    it("Should execute a successful proposal", async function () {
      // Fast forward time
      await ethers.provider.send("evm_increaseTime", [8 * 86400]);
      await ethers.provider.send("evm_mine");
      
      await expect(governance.connect(addr3).executeProposal(1))
        .to.emit(governance, "ProposalExecuted")
        .withArgs(1);

      const proposal = await governance.proposals(1);
      expect(proposal.status).to.equal(1); // APPROVED
      expect(proposal.executed).to.equal(true);
    });

    it("Should execute a rejected proposal", async function () {
      // Change votes to majority no
      await governance.connect(addr2).castVote(1, false);
      
      // Fast forward time
      await ethers.provider.send("evm_increaseTime", [8 * 86400]);
      await ethers.provider.send("evm_mine");
      
      await governance.connect(addr3).executeProposal(1);

      const proposal = await governance.proposals(1);
      expect(proposal.status).to.equal(2); // REJECTED
      expect(proposal.executed).to.equal(true);
    });

    it("Should fail to execute with invalid conditions", async function () {
      await expect(governance.connect(addr3).executeProposal(1))
        .to.be.revertedWith("Voting period not ended");
      
      // Fast forward time
      await ethers.provider.send("evm_increaseTime", [8 * 86400]);
      await ethers.provider.send("evm_mine");
      
      await governance.connect(addr3).executeProposal(1);
      
      await expect(governance.connect(addr3).executeProposal(1))
        .to.be.revertedWith("Proposal already executed");
    });
  });

  describe("Proposal Cancellation", function () {
    beforeEach(async function () {
      await governance.connect(addr1).createProposal(1, "Title", "Description", 0, 0, 7);
    });

    it("Should cancel a proposal as proposer", async function () {
      await expect(governance.connect(addr1).cancelProposal(1))
        .to.emit(governance, "ProposalCancelled")
        .withArgs(1);

      const proposal = await governance.proposals(1);
      expect(proposal.status).to.equal(4); // CANCELLED
    });

    it("Should cancel a proposal as owner", async function () {
      await expect(governance.cancelProposal(1))
        .to.emit(governance, "ProposalCancelled")
        .withArgs(1);

      const proposal = await governance.proposals(1);
      expect(proposal.status).to.equal(4); // CANCELLED
    });

    it("Should fail to cancel with invalid conditions", async function () {
      await expect(governance.connect(addr2).cancelProposal(1))
        .to.be.revertedWith("Not authorized");
      
      await governance.connect(addr1).cancelProposal(1);
      
      await expect(governance.connect(addr1).cancelProposal(1))
        .to.be.revertedWith("Proposal is not active");
    });
  });

  describe("Threshold Updates", function () {
    it("Should update proposal threshold", async function () {
      const newThreshold = 200; // 2%
      
      await expect(governance.updateProposalThreshold(newThreshold))
        .to.emit(governance, "ThresholdUpdated")
        .withArgs("ProposalThreshold", newThreshold);
      
      expect(await governance.proposalThreshold()).to.equal(newThreshold);
    });

    it("Should update approval threshold", async function () {
      const newThreshold = 6000; // 60%
      
      await expect(governance.updateApprovalThreshold(newThreshold))
        .to.emit(governance, "ThresholdUpdated")
        .withArgs("ApprovalThreshold", newThreshold);
      
      expect(await governance.approvalThreshold()).to.equal(newThreshold);
    });

    it("Should fail to update thresholds with invalid values", async function () {
      await expect(governance.updateProposalThreshold(0))
        .to.be.revertedWith("Invalid threshold value");
      
      await expect(governance.updateProposalThreshold(1001))
        .to.be.revertedWith("Invalid threshold value");
      
      await expect(governance.updateApprovalThreshold(4999))
        .to.be.revertedWith("Invalid threshold value");
      
      await expect(governance.updateApprovalThreshold(7501))
        .to.be.revertedWith("Invalid threshold value");
    });

    it("Should fail if not owner", async function () {
      await expect(governance.connect(addr1).updateProposalThreshold(200))
        .to.be.revertedWith("Ownable: caller is not the owner");
      
      await expect(governance.connect(addr1).updateApprovalThreshold(6000))
        .to.be.revertedWith("Ownable: caller is not the owner");
    });
  });
}); 