module.exports = {
    verificationCommands: `
    // Get property details
    const property = await PropertyInfo.getProperty(1)
    
    // Get user details
    const user = await UserInfo.getUserInfo({from: accounts[1]})
    
    // Get owner properties
    const ownerProperties = await PropertyInfo.getOwnerProperties(accounts[1])
    
    // Transfer property
    await PropertyInfo.transferProperty(1, accounts[3], {from: accounts[1]})
    `,
    
    propertyTransferDemo: `
    // Show initial owner
    const initialOwner = (await PropertyInfo.getProperty(1)).owner
    console.log("Initial owner:", initialOwner)
    
    // Transfer property
    await PropertyInfo.transferProperty(1, accounts[3], {from: accounts[1]})
    
    // Show new owner
    const newOwner = (await PropertyInfo.getProperty(1)).owner
    console.log("New owner:", newOwner)
    `
}