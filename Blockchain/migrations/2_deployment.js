const UserInfo = artifacts.require("UserInfo");

module.exports = function (deployer) {
    deployer.deploy(UserInfo);
};