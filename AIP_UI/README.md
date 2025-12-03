<<<<<<< HEAD
# Employee Diary Application

A React application for managing employee activities and records.

## Features

- View employee activities and records
- Filter activities by employee, category, or search terms
- Add new activity records
- Edit and delete existing records
- Sync with external systems (simulated)

## Tech Stack

- React with TypeScript
- Vite for frontend build and development
- Tailwind CSS for styling
- React Hook Form with Zod for form validation
- JSON Server for mock API

## Getting Started

### Prerequisites

- Node.js 18 or later
- npm or yarn

### Installation

1. Clone this repository
2. Install dependencies:

```bash
npm install
# or
yarn install
```

### Running the Development Server

This project uses JSON Server to simulate a backend API with the data from `db.json`.

1. Start the mock API server:

```bash
npm run mock-api
# or
yarn mock-api
```

This will start a server on http://localhost:3001

2. In a separate terminal, start the development server:

```bash
npm run dev
# or
yarn dev
```

This will start the Vite development server.

## API Endpoints

The mock API provides the following endpoints:

- `GET /employees` - List all employees
- `GET /activities` - List all activities
- `GET /activities?employeeId={id}` - List activities for a specific employee
- `POST /activities` - Create a new activity
- `PUT /activities/{id}` - Update an activity
- `DELETE /activities/{id}` - Delete an activity
- `GET /sync-status` - Get sync status for all sources

## Project Structure

- `src/components/` - Reusable React components
- `src/pages/` - Main pages of the application
- `src/services/` - Service layers for API interactions
- `src/types/` - TypeScript type definitions
- `src/config/` - Application configuration
- `db.json` - Mock database used by JSON Server
=======
<<<<<<< HEAD
# Advantage One Interactive Portal (AIP)

Advantage One Interactive Portal (AIP) is an advanced, full-featured crime and incident management software designed to streamline and automate various aspects of incident reporting, management, and follow-up. Built as an API-centric solution, AIP leverages a modern technology stack including React, Vite, .NET (with Entity Framework), and MSSQL for robust, scalable, and high-performance functionality across various modules.

## Key Features

### Crime & Incident Management
- Capture, track, and resolve incidents efficiently
- Enhanced user workflows
- Comprehensive incident tracking system

### CRM (Customer Relationship Management)
- Manage client interactions
- Track communication
- Ensure smooth information flow

### CBT (Computer-Based Training)
- Implement training programs for personnel
- Ensure staff are properly equipped to handle incidents

### Additional Features
- Incident Reporting
- Satisfaction Surveys
- Advanced Reporting
- Stock Management
- Recruitment Management
- Holiday Management

## Technologies Used

### Frontend
- React with TypeScript
- Vite for fast, modern development
- TailwindCSS for styling
- React Hook Form with Zod for validation

### Backend
- .NET – Robust and scalable API
- Entity Framework for ORM
- MSSQL Database
- CORS enabled for secure cross-origin requests

## Getting Started

### Prerequisites
- Node.js 18 or later
- .NET SDK
- MSSQL Server
- npm or yarn

### Installation

1. Clone the repository:
```bash
git clone https://github.com/dibanga2800/aip.git
```

2. Install Frontend Dependencies:
```bash
cd frontend
npm install
```

3. Install Backend Dependencies:
```bash
cd backend
dotnet restore
```

### Configuration

1. Database Setup:
- Configure your MSSQL connection string in `appsettings.json`
- Update any necessary database migrations using Entity Framework

2. CORS Configuration:
- CORS is enabled to allow cross-origin requests
- Update CORS policy in backend if needed

### Running the Application

1. Start the Backend:
```bash
cd backend
dotnet run
```

2. Start the Frontend:
```bash
cd frontend
=======
Advantage One Interactive Portal (AIP) is an advanced, full-featured crime and incident management software designed to streamline and automate various aspects of incident reporting, management, and follow-up. Built as an API-centric solution, AIP leverages a modern technology stack including React, Vite, and a .NET backend to deliver fast, scalable, and robust functionality across various modules.

Key Features:
Crime & Incident Management: Capture, track, and resolve incidents efficiently with features designed to enhance user workflows.

CRM (Customer Relationship Management): Manage client interactions, track communication, and ensure a smooth flow of information.

CBT (Computer-Based Training): Implement training programs for personnel, ensuring staff are properly equipped to handle incidents.

Incident Reporting: Users can easily submit reports with incident details, creating an efficient tracking system.

Satisfaction Surveys: Collect feedback from stakeholders to measure satisfaction and identify areas for improvement.

Advanced Reporting: Generate reports with detailed insights, aiding in decision-making and transparency.

Stock Management: Manage and track inventory, ensuring that necessary resources are available when needed.

Recruitment Management: Handle recruitment processes, from applications to selection, helping manage staffing needs.

Holiday Management: Plan and track employee holidays for improved workforce scheduling.

Technologies Used:
Frontend: React, Vite – Fast, modern frontend framework for building interactive UIs.

Backend: .NET – Robust and scalable backend API for handling complex business logic.

Database: (Specify the type of database you are using, e.g., SQL Server, PostgreSQL, etc.)

Authentication: (e.g., JWT, OAuth, or any authentication method you are using)

Why AIP?
AIP is designed to be a comprehensive solution that addresses the needs of both public safety agencies and organizations dealing with crime and incident management. Whether it's tracking a reported crime, managing employee schedules, or gathering stakeholder feedback, AIP empowers organizations to streamline operations and improve overall efficiency. The flexible, modular architecture ensures that the platform can evolve and scale as new features or requirements emerge.

Getting Started:
To get started with AIP, follow these instructions:

Clone the Repository:

bash
Copy
git clone https://github.com/dibanga2800/aip.git
Install Dependencies:

For the frontend, run:

bash
Copy
cd frontend
npm install
For the backend, run:

bash
Copy
cd backend
dotnet restore
Run the Project:

Frontend:

bash
Copy
>>>>>>> 91431598b13147046f51a005e491afd05a206b7d
npm run dev
Backend:

<<<<<<< HEAD
## Project Structure

- `frontend/` - React application with Vite
  - `src/components/` - Reusable React components
  - `src/pages/` - Main application pages
  - `src/services/` - API service layers
  - `src/types/` - TypeScript type definitions

- `backend/` - .NET API
  - `Controllers/` - API endpoints
  - `Models/` - Entity Framework models
  - `Services/` - Business logic
  - `Data/` - Database context and migrations

## Contributing

We welcome contributions to improve AIP. Please feel free to:
1. Fork the repository
2. Create a feature branch
3. Submit a pull request

## Contact

For questions or support:
- Open an issue on GitHub
- Email: dibanga2800@gmail.com

## License

This project is licensed under the MIT License - see the LICENSE.md file for details.
=======
bash
Copy
dotnet run
Configuration:
Update configuration files as necessary (e.g., database connections, API keys).

Contributing:
We welcome contributions to improve Advantage One Interactive Portal (AIP). If you have ideas, fixes, or features to propose, feel free to fork the repository, create a branch, and submit a pull request. Please refer to the CONTRIBUTING.md for contribution guidelines.

License:
This project is licensed under the MIT License - see the LICENSE.md file for details.

Contact:
For questions or support, feel free to open an issue on GitHub or reach out to dibanga2800@gmail.com.

This description is designed to be informative, easy to understand, and welcoming to potential contributors. Let me know if you'd like to adjust or add any more details!


You said:
the database is MSSQL and i will be using entity framework and CORS in the project

ChatGPT said:
Great! Here's an updated version of the GitHub project description incorporating your use of MSSQL, Entity Framework, and CORS:

Advantage One Interactive Portal (AIP)
Advantage One Interactive Portal (AIP) is an advanced, full-featured crime and incident management software designed to streamline and automate various aspects of incident reporting, management, and follow-up. Built as an API-centric solution, AIP leverages a modern technology stack including React, Vite, .NET (with Entity Framework), and MSSQL for robust, scalable, and high-performance functionality across various modules.

Key Features:
Crime & Incident Management: Capture, track, and resolve incidents efficiently with features designed to enhance user workflows.

CRM (Customer Relationship Management): Manage client interactions, track communication, and ensure a smooth flow of information.

CBT (Computer-Based Training): Implement training programs for personnel, ensuring staff are properly equipped to handle incidents.

Incident Reporting: Users can easily submit reports with incident details, creating an efficient tracking system.

Satisfaction Surveys: Collect feedback from stakeholders to measure satisfaction and identify areas for improvement.

Advanced Reporting: Generate reports with detailed insights, aiding in decision-making and transparency.

Stock Management: Manage and track inventory, ensuring that necessary resources are available when needed.

Recruitment Management: Handle recruitment processes, from applications to selection, helping manage staffing needs.

Holiday Management: Plan and track employee holidays for improved workforce scheduling.

Technologies Used:
Frontend: React, Vite – Fast, modern frontend framework for building interactive UIs.

Backend: .NET – Robust and scalable backend API for handling complex business logic.

Database: MSSQL – A reliable, high-performance relational database for managing and querying data.

ORM: Entity Framework – A powerful Object-Relational Mapper (ORM) for seamless database interaction with C#.

Authentication: (e.g., JWT, OAuth, or any authentication method you are using)

CORS: Cross-Origin Resource Sharing (CORS) configured to allow secure interactions between frontend and backend during development and production.

Why AIP?
AIP is designed to be a comprehensive solution that addresses the needs of both public safety agencies and organizations dealing with crime and incident management. Whether it's tracking a reported crime, managing employee schedules, or gathering stakeholder feedback, AIP empowers organizations to streamline operations and improve overall efficiency. The flexible, modular architecture ensures that the platform can evolve and scale as new features or requirements emerge.

Getting Started:
To get started with AIP, follow these instructions:

Clone the Repository:

bash
Copy
git clone https://github.com/dibanga2800/aip.git
Install Dependencies:

For the frontend, run:

bash
Copy
cd frontend
npm install
For the backend, run:

bash
Copy
cd backend
dotnet restore
Configure the Database:
Ensure that your MSSQL database is set up correctly. You can configure your connection string in appsettings.json (for the backend), and update any database migration commands using Entity Framework.

Run the Project:

Frontend:

bash
Copy
npm run dev
Backend:

bash
Copy
dotnet run
CORS Configuration:
CORS is enabled to allow cross-origin requests between your frontend and backend. Make sure the frontend runs on the correct URL or update the CORS policy in the backend to match your needs.

Configuration:
Update any configuration files as necessary, such as API keys, database connections, and authentication settings.

Contributing:
We welcome contributions to improve Advantage One Interactive Portal (AIP). If you have ideas, fixes, or features to propose, feel free to fork the repository, create a branch, and submit a pull request. Please refer to the CONTRIBUTING.md for contribution guidelines.

License:
This project is licensed under the MIT License - see the LICENSE.md file for details.

Contact:
For questions or support, feel free to open an issue on GitHub or reach out to dibanga2800@gmail.com.

>>>>>>> 91431598b13147046f51a005e491afd05a206b7d
>>>>>>> master
