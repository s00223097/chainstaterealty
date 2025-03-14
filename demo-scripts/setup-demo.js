const PropertyInfo = artifacts.require("PropertyInfo");
const UserInfo = artifacts.require("UserInfo");

const testProperties = [
    {
        location: "123 Blockchain Ave, Crypto City",
        specifications: "3 bed, 2 bath, 2000 sqft, Modern Villa",
        rentalStatus: false,
        maintenanceSchedule: "Monthly inspection, Quarterly deep cleaning",
        photos: [
            "ipfs://QmHash1",
            "ipfs://QmHash2"
        ]
    },
    {
        location: "456 Digital Lane, Web3 Valley",
        specifications: "2 bed, 2 bath, 1500 sqft, Luxury Apartment",
        rentalStatus: true,
        maintenanceSchedule: "Bi-monthly inspection, Annual renovation",
        photos: [
            "ipfs://QmHash3",
            "ipfs://QmHash4"
        ]
    }
];

const testUsers = [
    {
        firstname: "John",
        lastname: "Blockchain",
        email: "john@blockchain.com",
        kycDocumentHash: "QmHashKYC1",
        bankingInfoEncrypted: "encrypted_banking_info_1"
    },
    {
        firstname: "Alice",
        lastname: "Crypto",
        email: "alice@crypto.com",
        kycDocumentHash: "QmHashKYC2",
        bankingInfoEncrypted: "encrypted_banking_info_2"
    }
];

module.exports = async function(callback) {
    try {
        // Get deployed contracts
        const propertyInfo = await PropertyInfo.deployed();
        const userInfo = await UserInfo.deployed();

        // Get accounts
        const accounts = await web3.eth.getAccounts();
        const admin = accounts[0];
        const propertyOwner1 = accounts[1];
        const propertyOwner2 = accounts[2];
        const buyer1 = accounts[3];
        const buyer2 = accounts[4];

        console.log("Setting up test data...");

        // Register users
        console.log("Registering users...");
        await userInfo.registerUser(
            testUsers[0].firstname,
            testUsers[0].lastname,
            testUsers[0].email,
            testUsers[0].kycDocumentHash,
            testUsers[0].bankingInfoEncrypted,
            {from: propertyOwner1}
        );

        await userInfo.registerUser(
            testUsers[1].firstname,
            testUsers[1].lastname,
            testUsers[1].email,
            testUsers[1].kycDocumentHash,
            testUsers[1].bankingInfoEncrypted,
            {from: propertyOwner2}
        );

        // Register properties
        console.log("Registering properties...");
        await propertyInfo.registerProperty(
            testProperties[0].location,
            testProperties[0].specifications,
            testProperties[0].rentalStatus,
            testProperties[0].maintenanceSchedule,
            testProperties[0].photos,
            {from: propertyOwner1}
        );

        await propertyInfo.registerProperty(
            testProperties[1].location,
            testProperties[1].specifications,
            testProperties[1].rentalStatus,
            testProperties[1].maintenanceSchedule,
            testProperties[1].photos,
            {from: propertyOwner2}
        );

        console.log("Setup complete!");

        // Verify setup
        console.log("\nVerifying setup...");
        const property1 = await propertyInfo.getProperty(1);
        console.log("Property 1:", property1);

        const user1Info = await userInfo.getUserInfo({from: propertyOwner1});
        console.log("User 1:", user1Info);

        callback();
    } catch (error) {
        console.error("Error:", error);
        callback(error);
    }
}