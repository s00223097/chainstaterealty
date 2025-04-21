// SPDX-License-Identifier: MIT
pragma solidity ^0.8.19;

contract UserInfo {
    address public owner;
    
    struct User 
    {
        string firstname;
        string lastname;
        string email;
        string kycDocumentHash; // Reference to off-chain storage (e.g., IPFS hash)
        string bankingInfoEncrypted; // Encrypted banking information
    }
    
    mapping(address => User) private users;
    
    constructor() 
    {
        owner = msg.sender; // Set contract deployer as owner
    }

    modifier onlyOwner() 
    {
        require(msg.sender == owner, "Not authorized");
        _;
    }
    
    function registerUser(string memory _firstname, string memory _lastname, string memory _email, string memory _kycDocumentHash,  string memory _bankingInfoEncrypted) public 
    {
        users[msg.sender] = User(_firstname, _lastname, _email, _kycDocumentHash, _bankingInfoEncrypted);
    }
    
    function getUserInfo() public view returns (string memory, string memory, string memory) 
    {
        User memory u = users[msg.sender]; // Retrieve user correctly
        return (u.firstname, u.lastname, u.email);
    }
}