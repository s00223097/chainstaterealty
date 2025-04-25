const { expect } = require('chai');
const { ethers } = require('hardhat');

let KYCVerification;
let kycVerification;
let owner;
let addr1;
let addr2;

beforeEach(async function () {
  KYCVerification = await ethers.getContractFactory('KYCVerification');
  [owner, addr1, addr2] = await ethers.getSigners();
  kycVerification = await KYCVerification.deploy();
  await kycVerification.deployed();
});

describe('KYCVerification', function () {
  it('should deploy the contract and assign roles', async function () {
    expect(await kycVerification.hasRole(await kycVerification.DEFAULT_ADMIN_ROLE(), owner.address)).to.be.true;
    expect(await kycVerification.hasRole(await kycVerification.ADMIN_ROLE(), owner.address)).to.be.true;
    expect(await kycVerification.hasRole(await kycVerification.VERIFIER_ROLE(), owner.address)).to.be.true;
  });

  it('should verify an investor', async function () {
    await kycVerification.verifyInvestor(addr1.address, 1, 30, 'hash');
    const investorData = await kycVerification.investors(addr1.address);
    expect(investorData.isActive).to.be.true;
    expect(investorData.level).to.equal(1);
  });

  it('should revoke verification', async function () {
    await kycVerification.verifyInvestor(addr1.address, 1, 30, 'hash');
    await kycVerification.revokeVerification(addr1.address, 'reason');
    const investorData = await kycVerification.investors(addr1.address);
    expect(investorData.isActive).to.be.false;
  });

  it('should blacklist an investor', async function () {
    await kycVerification.blacklistInvestor(addr1.address, 'reason');
    expect(await kycVerification.blacklist(addr1.address)).to.be.true;
  });

  it('should unblacklist an investor', async function () {
    await kycVerification.blacklistInvestor(addr1.address, 'reason');
    await kycVerification.unblacklistInvestor(addr1.address);
    expect(await kycVerification.blacklist(addr1.address)).to.be.false;
  });

  it('should update country restriction', async function () {
    await kycVerification.updateCountryRestriction('US', true);
    expect(await kycVerification.restrictedCountries('US')).to.be.true;
  });
});