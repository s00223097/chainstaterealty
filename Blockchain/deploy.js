// Script to deploy the contracts to Ganache and write the contract addresses to a file
// truffle exec deploy.js --network development

const fs = require('fs');
const path = require('path');

// Import contract artifacts
const PropertyInfo = artifacts.require("PropertyInfo");
const UserInfo = artifacts.require("UserInfo");

module.exports = async function(callback) {
  try {
    console.log('Deploying contracts to Ganache...');
    
    // Get accounts from Ganache
    const accounts = await web3.eth.getAccounts();
    console.log(`Using account: ${accounts[0]}`);
    
    // UserInfo contract
    console.log('Deploying UserInfo contract...');
    const userInfo = await UserInfo.new({ from: accounts[0] });
    console.log(`UserInfo contract deployed at: ${userInfo.address}`);
    
    // PropertyInfo contract
    console.log('Deploying PropertyInfo contract...');
    const propertyInfo = await PropertyInfo.new(userInfo.address, { from: accounts[0] });
    console.log(`PropertyInfo contract deployed at: ${propertyInfo.address}`);
    
    // Write contract addresses to a file
    const deploymentInfo = {
      userInfoAddress: userInfo.address,
      propertyInfoAddress: propertyInfo.address,
      deployedBy: accounts[0],
      deploymentTime: new Date().toISOString()
    };
    
    const filePath = path.join(__dirname, 'deployment-info.json');
    fs.writeFileSync(filePath, JSON.stringify(deploymentInfo, null, 2));
    console.log(`Deployment info written to ${filePath}`);
    
    // Sample API request to update the contract addresses
    const apiRequestExample = `
POST http://localhost:5001/api/blockchain/set-contracts
Content-Type: application/json

{
  "propertyAddress": "${propertyInfo.address}",
  "userAddress": "${userInfo.address}"
}
`;
    
    const apiRequestPath = path.join(__dirname, 'update-contracts-api-request.http');
    fs.writeFileSync(apiRequestPath, apiRequestExample);
    console.log(`API request example written to ${apiRequestPath}`);
    
    console.log('Deployment completed successfully!');
    callback();
  } catch (error) {
    console.error('Error during deployment:', error);
    callback(error);
  }
}; 