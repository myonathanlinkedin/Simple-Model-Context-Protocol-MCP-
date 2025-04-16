# Simple Model Context Protocol MCP

This project is a simple demonstration of the **Model Context Protocol (MCP)**, designed to facilitate seamless integration between clients and servers. It provides a standardized way for large language models (LLMs) to interact with external tools and data sources. This project showcases the use of MCP for client-server communication, CRUD operations, and integrating model context in applications.

## What is the Model Context Protocol (MCP)?

The **Model Context Protocol (MCP)** is an open protocol that enables easy integration of external tools and data sources with large language models (LLMs). It provides a common standard for exchanging model context, making it easier to build context-aware AI applications, such as:

- AI-powered IDEs
- Custom AI workflows
- Chat interfaces with real-time context updating

MCP ensures that LLMs have access to the context they need by connecting them with external data and tools.

## Features

This project demonstrates the following capabilities:
- **Client-Server Communication**: Uses MCP to facilitate communication between the client and server.
- **CRUD Operations**: Allows creating, reading, updating, and deleting notes stored in an SQLite database.
- **Random Seed Generation and Echo**: Includes a tool for generating a random seed and echoing it back along with the input message.
- **SQLite Database Integration**: Demonstrates how to interact with an SQLite database using Entity Framework Core (EF Core) for storing and retrieving notes.

## Project Structure

- **MCPServer**: Contains the server-side implementation, including CRUD operations on notes and the echo tool.
- **MCPClient**: Simulates a client that communicates with the server using the MCP protocol.
- **ModelContextProtocol**: The core protocol library that defines the standards for model context exchange.

## Technologies Used

- **C#** (.NET Core 9.0)
- **SQLite**: A lightweight database used for storing notes.
- **Entity Framework Core**: ORM used for data access and manipulation.
- **MCP Protocol**: Standard protocol for client-server communication in AI-powered applications.
