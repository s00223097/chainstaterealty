const Test = artifacts.require("Test");

module.exports = function (deployer) {
    deployer.deploy(Test, "Hello, Blockchain!"); // Pass initial message
};