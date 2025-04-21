module.exports = {
  networks: {
      development: {
          host: "127.0.0.1", // Ganache GUI default host
          port: 7545,        // Ganache GUI default port
          network_id: "5777", // Ganache GUI default network ID
          gas: 6721975,       // Increase gas limit for contract deployment
          gasPrice: 20000000000, // 20 Gwei
      },
  },
  compilers: {
      solc: {
          version: "0.8.19", // Match Solidity version
      },
  },
};