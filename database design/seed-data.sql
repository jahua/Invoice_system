-- ======================================================================
-- seed data for invoicing system
-- ======================================================================

-- gotta enable IDENTITY_INSERT to use specific ids
SET IDENTITY_INSERT Employees ON;

-- seed employees table
INSERT INTO Employees (EmployeeID, UniqueIdentifier, FirstName, LastName, Email, PhoneNumber, Department, Position, Salary, HireDate)
VALUES 
    (1, 'EMP001', 'Robert', 'Wilson', 'robert.wilson@company.com', '+1-555-0101', 'Management', 'Invoice Manager', 95000.00, '2022-01-15'),
    (2, 'EMP002', 'John', 'Doe', 'john.doe@company.com', '+1-555-0102', 'Engineering', 'Senior Developer', 85000.00, '2022-02-01'),
    (3, 'EMP003', 'Jane', 'Smith', 'jane.smith@company.com', '+1-555-0103', 'Design', 'UX Designer', 75000.00, '2022-03-15'),
    (4, 'EMP004', 'Mike', 'Johnson', 'mike.johnson@company.com', '+1-555-0104', 'Engineering', 'Software Engineer', 70000.00, '2022-04-01');

SET IDENTITY_INSERT Employees OFF;

-- seed users (all pwds are hashed with bcrypt  all are same'password123' )
SET IDENTITY_INSERT Users ON;

INSERT INTO Users (UserID, Username, Email, PasswordHash, Role, EmployeeID) 
VALUES 
    (1, 'manager1', 'robert.wilson@company.com', '$2a$11$QkOQegZU1CKGQEXBlcjXROujzqHHZBxNZ7Ey7Oy4DpZZVoQC3YFEC', 'InvoiceManager', 1),
    (2, 'employee1', 'john.doe@company.com', '$2a$11$QkOQegZU1CKGQEXBlcjXROujzqHHZBxNZ7Ey7Oy4DpZZVoQC3YFEC', 'Employee', 2),
    (3, 'employee2', 'jane.smith@company.com', '$2a$11$QkOQegZU1CKGQEXBlcjXROujzqHHZBxNZ7Ey7Oy4DpZZVoQC3YFEC', 'Employee', 3),
    (4, 'employee3', 'mike.johnson@company.com', '$2a$11$QkOQegZU1CKGQEXBlcjXROujzqHHZBxNZ7Ey7Oy4DpZZVoQC3YFEC', 'Employee', 4);

SET IDENTITY_INSERT Users OFF;

-- seed contracts
SET IDENTITY_INSERT Contracts ON;

INSERT INTO Contracts (ContractID, EmployeeID, StartDate, EndDate, DailyRate, ContractType, PayGrade)
VALUES 
    -- robert (mgr) - fulltime expert
    (1, 1, '2022-01-15', '2024-01-14', 500.00, 0, 3),
    
    -- john - multiple contracts showing growth
    (2, 2, '2022-02-01', '2023-01-31', 400.00, 0, 2), -- fulltime sr
    (3, 2, '2023-02-01', '2024-01-31', 450.00, 0, 2), -- got a raise
    
    -- jane - contract worker
    (4, 3, '2022-03-15', '2023-03-14', 350.00, 1, 1), -- intermediate
    (5, 3, '2023-03-15', '2024-03-14', 400.00, 1, 2), -- promotion!
    
    -- mike - freelancer to ft
    (6, 4, '2022-04-01', '2023-03-31', 300.00, 2, 0), -- freelance junior
    (7, 4, '2023-04-01', '2024-03-31', 350.00, 0, 1); -- now fulltime

SET IDENTITY_INSERT Contracts OFF;

-- seed invoices
SET IDENTITY_INSERT Invoices ON;

INSERT INTO Invoices (InvoiceID, InvoiceNumber, EmployeeID, ContractID, StartDate, EndDate, DaysWorked, TotalAmount, Status, CreatedAt)
VALUES 
    -- johns invoices
    (1, 'INV-2023-001', 2, 2, '2023-01-01', '2023-01-31', 22, 8800.00, 2, '2023-02-01'), -- approved
    (2, 'INV-2023-002', 2, 3, '2023-02-01', '2023-02-28', 20, 9000.00, 2, '2023-03-01'), -- approved
    
    -- janes invoices
    (3, 'INV-2023-003', 3, 4, '2023-02-01', '2023-02-28', 18, 6300.00, 2, '2023-03-01'), -- approved
    (4, 'INV-2023-004', 3, 5, '2023-03-15', '2023-04-14', 22, 8800.00, 1, '2023-04-15'), -- submitted
    
    -- mikes invoices
    (5, 'INV-2023-005', 4, 6, '2023-03-01', '2023-03-31', 23, 6900.00, 2, '2023-04-01'), -- approved
    (6, 'INV-2023-006', 4, 7, '2023-04-01', '2023-04-30', 21, 7350.00, 0, '2023-05-01'), -- draft
    
    -- more recent ones
    (7, 'INV-2023-007', 2, 3, '2023-03-01', '2023-03-31', 23, 10350.00, 3, '2023-04-01'), -- rejected - oops!
    (8, 'INV-2023-008', 3, 5, '2023-04-15', '2023-05-14', 20, 8000.00, 1, '2023-05-15'); -- submitted

SET IDENTITY_INSERT Invoices OFF;

-- done!
PRINT 'Seed data inserted successfully.';
-- ======================================================================