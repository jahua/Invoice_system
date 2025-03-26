-- ======================================================================
-- database schema for online invoicing system (company xyz)
-- Target engine: SQL Server
-- ======================================================================

-- drop existing tables in reverse order (optional for clean slate)
-- use with caution cuz it deletes everything!!
/*
IF OBJECT_ID('dbo.Invoices', 'U') IS NOT NULL DROP TABLE dbo.Invoices;
IF OBJECT_ID('dbo.Contracts', 'U') IS NOT NULL DROP TABLE dbo.Contracts;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
IF OBJECT_ID('dbo.Employees', 'U') IS NOT NULL DROP TABLE dbo.Employees;
*/

-- 1. employees table
-- stores basic info about employees
CREATE TABLE Employees (
    EmployeeID INT IDENTITY(1,1) PRIMARY KEY,
    UniqueIdentifier NVARCHAR(50) NOT NULL UNIQUE,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    PhoneNumber NVARCHAR(20),
    Department NVARCHAR(100) NOT NULL,
    Position NVARCHAR(100) NOT NULL,
    Salary DECIMAL(18, 2) NOT NULL,
    HireDate DATE NOT NULL
);
PRINT 'Table Employees created.';

-- 2. users table
-- stores login credentials n roles, linked to employees
CREATE TABLE Users (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(100) NOT NULL UNIQUE,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    Role NVARCHAR(50) NOT NULL,
    EmployeeID INT NOT NULL UNIQUE,

    CONSTRAINT FK_Users_Employees FOREIGN KEY (EmployeeID) REFERENCES Employees(EmployeeID),
    CONSTRAINT CK_UserRole CHECK (Role IN ('Employee', 'InvoiceManager'))
);
PRINT 'Table Users created.';

-- 3. contracts table
-- stores employee contract stuff, validity and pay rate etc
CREATE TABLE Contracts (
    ContractID INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeID INT NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    DailyRate DECIMAL(18, 2) NOT NULL,
    ContractType INT NOT NULL,
    PayGrade INT NOT NULL,

    CONSTRAINT FK_Contracts_Employees FOREIGN KEY (EmployeeID) REFERENCES Employees(EmployeeID),
    CONSTRAINT CK_ContractDates CHECK (EndDate >= StartDate),
    CONSTRAINT CK_ContractType CHECK (ContractType IN (0, 1, 2)),
    CONSTRAINT CK_PayGrade CHECK (PayGrade IN (0, 1, 2, 3))
);
PRINT 'Table Contracts created.';

-- 4. invoices table
-- stores submitted invoices
CREATE TABLE Invoices (
    InvoiceID INT IDENTITY(1,1) PRIMARY KEY,
    InvoiceNumber NVARCHAR(50) NOT NULL UNIQUE,
    EmployeeID INT NOT NULL,
    ContractID INT NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    DaysWorked INT NOT NULL,
    TotalAmount DECIMAL(18, 2) NOT NULL,
    Status INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_Invoices_Employees FOREIGN KEY (EmployeeID) REFERENCES Employees(EmployeeID),
    CONSTRAINT FK_Invoices_Contracts FOREIGN KEY (ContractID) REFERENCES Contracts(ContractID),
    CONSTRAINT CK_InvoiceDates CHECK (EndDate >= StartDate),
    CONSTRAINT CK_DaysWorkedPositive CHECK (DaysWorked > 0),
    CONSTRAINT CK_InvoiceStatus CHECK (Status IN (0, 1, 2, 3))
);
PRINT 'Table Invoices created.';

-- ======================================================================
-- indexes for perf
-- ======================================================================

-- indexes on FKs
CREATE INDEX IX_Users_EmployeeID ON Users(EmployeeID);
CREATE INDEX IX_Contracts_EmployeeID ON Contracts(EmployeeID);
CREATE INDEX IX_Invoices_EmployeeID ON Invoices(EmployeeID);
CREATE INDEX IX_Invoices_ContractID ON Invoices(ContractID);

-- indexes on stuff we query alot
CREATE INDEX IX_Users_Username ON Users(Username);
CREATE INDEX IX_Users_Email ON Users(Email);
CREATE INDEX IX_Contracts_Dates ON Contracts(EmployeeID, StartDate, EndDate);
CREATE INDEX IX_Invoices_Dates ON Invoices(EmployeeID, StartDate, EndDate);
CREATE INDEX IX_Invoices_Status ON Invoices(Status);

PRINT 'Indexes created.';
PRINT 'Database schema generation complete.';
-- ======================================================================