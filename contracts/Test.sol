// SPDX-License-Identifier: MIT
pragma solidity ^0.8.19;

contract Test {
    string public message;

    event ContractDeployed(address indexed owner, string initialMessage);

    constructor(string memory _message) {
        message = _message;
        emit ContractDeployed(msg.sender, _message); // Log contract deployment
    }

    function updateMessage(string memory _newMessage) public {
        message = _newMessage;
    }

        function getMessage() public view returns (string memory) 
    {
        return message;
    }
}