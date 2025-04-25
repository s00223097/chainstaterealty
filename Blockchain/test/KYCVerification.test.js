const { expect } = require('chai');
const { ethers } = require('hardhat');

describe('KYCVerification', function () {
  let KYCVerification;
  let kyc;
  let owner;
  let admin;
  let verifier;
  let investor;

  beforeEach(async function () {
    [owner, admin, verifier, investor] = await ethers.getSigners();
    
    KYCVerification = await ethers.getContractFactory('KYCVerification');
    kyc = await KYCVerification.deploy();
    await kyc.deployed();
    
    // Grant roles
    await kyc.grantRole(await kyc.ADMIN_ROLE(), admin.address);
    await kyc.grantRole(await kyc.VERIFIER_ROLE(), verifier.address);
  });

  describe('Deployment', function () {
    it('Should set the right roles', async function () {
      expect(await kyc.hasRole(await kyc.DEFAULT_ADMIN_ROLE(), owner.address)).to.equal(true);
      expect(await kyc.hasRole(await kyc.ADMIN_ROLE(), owner.address)).to.equal(true);
      expect(await kyc.hasRole(await kyc.VERIFIER_ROLE(), owner.address)).to.equal(true);
      expect(await kyc.hasRole(await kyc.ADMIN_ROLE(), admin.address)).to.equal(true);
      expect(await kyc.hasRole(await kyc.VERIFIER_ROLE(), verifier.address)).to.equal(true);
    });
  });

  describe('Investor Verification', function () {
    it('Should verify an investor', async function () {
      const level = 1; // BASIC
      const validityDays = 365;
      const verificationHash = 'QmHash123';
      
      await expect(kyc.connect(verifier).verifyInvestor(investor.address, level, validityDays, verificationHash))
        .to.emit(kyc, 'InvestorVerified')
        .withArgs(investor.address, level, (await ethers.provider.getBlock('latest')).timestamp + (validityDays * 86400));
      
      const data = await kyc.investors(investor.address);
      expect(data.wallet).to.equal(investor.address);
      expect(data.level).to.equal(level);
      expect(data.isActive).to.equal(true);
      expect(data.verificationHash).to.equal(verificationHash);
    });

    it('Should fail to verify with invalid parameters', async function () {
      await expect(kyc.connect(verifier).verifyInvestor(ethers.constants.AddressZero, 1, 365, 'QmHash123'))
        .to.be.revertedWith('Invalid address');
      
      await expect(kyc.connect(verifier).verifyInvestor(investor.address, 0, 365, 'QmHash123'))
        .to.be.revertedWith('Invalid verification level');
      
      await expect(kyc.connect(verifier).verifyInvestor(investor.address, 1, 0, 'QmHash123'))
        .to.be.revertedWith('Validity must be greater than zero');
      
      await expect(kyc.connect(verifier).verifyInvestor(investor.address, 1, 365, ''))
        .to.be.revertedWith('Verification hash required');
    });

    it('Should fail to verify blacklisted investor', async function () {
      await kyc.connect(admin).blacklistInvestor(investor.address, 'Fraudulent activity');
      
      await expect(kyc.connect(verifier).verifyInvestor(investor.address, 1, 365, 'QmHash123'))
        .to.be.revertedWith('Investor is blacklisted');
    });

    it('Should fail if not verifier', async function () {
      await expect(kyc.connect(investor).verifyInvestor(investor.address, 1, 365, 'QmHash123'))
        .to.be.revertedWith('AccessControl: account');
    });
  });

  describe('Verification Management', function () {
    beforeEach(async function () {
      await kyc.connect(verifier).verifyInvestor(investor.address, 1, 365, 'QmHash123');
    });

    it('Should revoke verification', async function () {
      await expect(kyc.connect(verifier).revokeVerification(investor.address, 'Suspicious activity'))
        .to.emit(kyc, 'VerificationRevoked')
        .withArgs(investor.address, 'Suspicious activity');
      
      const data = await kyc.investors(investor.address);
      expect(data.isActive).to.equal(false);
    });

    it('Should reject investor', async function () {
      await expect(kyc.connect(verifier).rejectInvestor(investor.address, 'Insufficient documentation'))
        .to.emit(kyc, 'InvestorRejected')
        .withArgs(investor.address, 'Insufficient documentation');
      
      const data = await kyc.investors(investor.address);
      expect(data.level).to.equal(0); // NONE
      expect(data.isActive).to.equal(false);
    });

    it('Should renew verification', async function () {
      const newValidityDays = 180;
      
      await expect(kyc.connect(verifier).renewVerification(investor.address, newValidityDays))
        .to.emit(kyc, 'VerificationRenewed')
        .withArgs(investor.address, (await ethers.provider.getBlock('latest')).timestamp + (newValidityDays * 86400));
      
      const data = await kyc.investors(investor.address);
      expect(data.expirationDate).to.equal((await ethers.provider.getBlock('latest')).timestamp + (newValidityDays * 86400));
    });

    it('Should fail to renew if not active', async function () {
      await kyc.connect(verifier).revokeVerification(investor.address, 'Test');
      
      await expect(kyc.connect(verifier).renewVerification(investor.address, 180))
        .to.be.revertedWith('Investor not active');
    });
  });

  describe('Blacklist Management', function () {
    it('Should blacklist an investor', async function () {
      await expect(kyc.connect(admin).blacklistInvestor(investor.address, 'Fraudulent activity'))
        .to.emit(kyc, 'InvestorBlacklisted')
        .withArgs(investor.address, 'Fraudulent activity');
      
      expect(await kyc.blacklist(investor.address)).to.equal(true);
    });

    it('Should unblacklist an investor', async function () {
      await kyc.connect(admin).blacklistInvestor(investor.address, 'Fraudulent activity');
      
      await expect(kyc.connect(admin).unblacklistInvestor(investor.address))
        .to.emit(kyc, 'InvestorUnblacklisted')
        .withArgs(investor.address);
      
      expect(await kyc.blacklist(investor.address)).to.equal(false);
    });

    it('Should fail if not admin', async function () {
      await expect(kyc.connect(verifier).blacklistInvestor(investor.address, 'Test'))
        .to.be.revertedWith('AccessControl: account');
      
      await expect(kyc.connect(verifier).unblacklistInvestor(investor.address))
        .to.be.revertedWith('AccessControl: account');
    });
  });

  describe('Country Restrictions', function () {
    it('Should update country restrictions', async function () {
      await expect(kyc.connect(admin).updateCountryRestriction('US', true))
        .to.emit(kyc, 'CountryRestrictionUpdated')
        .withArgs('US', true);
      
      expect(await kyc.isCountryRestricted('US')).to.equal(true);
    });

    it('Should fail if not admin', async function () {
      await expect(kyc.connect(verifier).updateCountryRestriction('US', true))
        .to.be.revertedWith('AccessControl: account');
    });
  });

  describe('Verification Checks', function () {
    beforeEach(async function () {
      await kyc.connect(verifier).verifyInvestor(investor.address, 1, 365, 'QmHash123');
    });

    it('Should check verification status', async function () {
      expect(await kyc.isVerified(investor.address, 1)).to.equal(true); // BASIC
      expect(await kyc.isVerified(investor.address, 2)).to.equal(false); // ACCREDITED
    });

    it('Should get verification details', async function () {
      const [level, verificationDate, expirationDate, isActive, isBlacklisted] = 
        await kyc.getVerificationDetails(investor.address);
      
      expect(level).to.equal(1); // BASIC
      expect(isActive).to.equal(true);
      expect(isBlacklisted).to.equal(false);
    });

    it('Should check expiration', async function () {
      expect(await kyc.hasExpired(investor.address)).to.equal(false);
      
      // Fast forward time
      await ethers.provider.send('evm_increaseTime', [366 * 86400]);
      await ethers.provider.send('evm_mine');
      
      expect(await kyc.hasExpired(investor.address)).to.equal(true);
    });
  });

  describe('Pausable', function () {
    it('Should pause and unpause verification', async function () {
      await kyc.connect(admin).pause();
      expect(await kyc.paused()).to.equal(true);
      
      await kyc.connect(admin).unpause();
      expect(await kyc.paused()).to.equal(false);
    });

    it('Should fail to verify when paused', async function () {
      await kyc.connect(admin).pause();
      
      await expect(kyc.connect(verifier).verifyInvestor(investor.address, 1, 365, 'QmHash123'))
        .to.be.revertedWith('Pausable: paused');
    });
  });
});