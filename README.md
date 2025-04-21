# chainstaterealty
Blockchain-based fractional real estate platform enabling property tokenization and trading using smart contracts. Built with C#, Ethereum and Avalonia UI.

Tech Stack
Backend
- .NET Web API
- JWT for API authentication possibly
- Entity Framework : Class libraries (e.g. ApiService)
- Swagger/OpenAPI for API docs
Client Application
- .NET MAUI
- C# backend / XAML UI
- UI designed with Material in Figma
Server
- Local
Database
- SQL Server.
Blockchain
- Solidity for contracts
- Nethereum for .NET for blockchain integration for the web api
- Web3.js/Ethers.js for blockchain interaction (although the app will not commit transactions on an large blockchain due to gas fees.)
- local Ganache/Hardhat network 


User Types
Primary User Type
Investor:
• Has capital to invest in real estate
• May be new to real estate investing
• Wants lower barrier to entry than traditional property ownership
• Tech-savvy enough to use blockchain-based platforms
• Could be either retail or professional investor

Other potential User Types:
Property Owners/Sellers
Property Managers
Real Estate Agents
Auditors? Getting into legalese here…
User Stories
(based on the primary user type).

"As an investor, I want to browse available properties with their details and photos so I can find investment opportunities"
"As an investor, I want to purchase and manage fractional shares of properties so I can build my real estate portfolio"
"As an investor, I want to complete identity verification once so I can trade property shares"
"As an investor, I want to view my complete transaction history so I can track my investments"
"As an investor, I want to see all my property investments in one place so I can track performance"