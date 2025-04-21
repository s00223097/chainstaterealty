// SPDX-License-Identifier: MIT
pragma solidity ^0.8.19;

contract PropertyInfo {
    struct Property {
        string location; // General area or exact address
        string specifications; // Property specifications (size, rooms, etc.)
        bool rentalStatus; // True if property is rented, false otherwise
        string maintenanceSchedule; // Off-chain reference for maintenance schedules
        string[] photos; // Off-chain reference for property photos
        address owner; // Owner of the property
    }

    mapping(uint256 => Property) public properties;
    mapping(address => uint256[]) public ownerProperties; // Track properties by owner
    uint256 public propertyCounter;

    event PropertyRegistered(uint256 id, address indexed owner);
    event PropertyUpdated(uint256 id, string location, string specifications, bool rentalStatus, string maintenanceSchedule, string[] photos);
    event PropertyTransferred(uint256 id, address indexed from, address indexed to);

    modifier onlyPropertyOwner(uint256 _id) {
        require(msg.sender == properties[_id].owner, "Caller is not the property owner");
        _;
    }

    function registerProperty(
        string memory _location,
        string memory _specifications,
        bool _rentalStatus,
        string memory _maintenanceSchedule,
        string[] memory _photos
    ) public {
        propertyCounter++;
        properties[propertyCounter] = Property({
            location: _location,
            specifications: _specifications,
            rentalStatus: _rentalStatus,
            maintenanceSchedule: _maintenanceSchedule,
            photos: _photos,
            owner: msg.sender
        });

        ownerProperties[msg.sender].push(propertyCounter);

        emit PropertyRegistered(propertyCounter, msg.sender);
    }

    function updateProperty(
        uint256 _id,
        string memory _location,
        string memory _specifications,
        bool _rentalStatus,
        string memory _maintenanceSchedule,
        string[] memory _photos
    ) external onlyPropertyOwner(_id) {
        Property storage prop = properties[_id];
        prop.location = _location;
        prop.specifications = _specifications;
        prop.rentalStatus = _rentalStatus;
        prop.maintenanceSchedule = _maintenanceSchedule;
        prop.photos = _photos;

        emit PropertyUpdated(_id, _location, _specifications, _rentalStatus, _maintenanceSchedule, _photos);
    }

    function transferProperty(uint256 _id, address _newOwner) public onlyPropertyOwner(_id) {
        require(_newOwner != address(0), "Invalid new owner");

        // Remove property from the current owner's list
        uint256[] storage owned = ownerProperties[msg.sender];
        for (uint256 i = 0; i < owned.length; i++) {
            if (owned[i] == _id) {
                owned[i] = owned[owned.length - 1]; // Replace with last element
                owned.pop(); // Remove last element
                break;
            }
        }

        // Transfer ownership
        properties[_id].owner = _newOwner;
        ownerProperties[_newOwner].push(_id);

        emit PropertyTransferred(_id, msg.sender, _newOwner);
    }

    function getProperty(uint256 _id) external view returns (
        string memory location,
        string memory specifications,
        bool rentalStatus,
        string memory maintenanceSchedule,
        string[] memory photos,
        address owner
    ) {
        Property memory prop = properties[_id];
        return (
            prop.location,
            prop.specifications,
            prop.rentalStatus,
            prop.maintenanceSchedule,
            prop.photos,
            prop.owner
        );
    }

    function getOwnerProperties(address _owner) external view returns (uint256[] memory) {
        return ownerProperties[_owner];
    }
}
