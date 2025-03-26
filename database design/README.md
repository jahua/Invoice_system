# Database Schema Design

this dir has the db schema design for Online Invoicing System (Company XYZ).

## Overview

schema supports an invoicing system where employees submit invoices based on contracts, and invoice managers can review/manage them.

## Files

- `schema.sql`: has complete db schema definition
- `seed-data.sql`: has sample data for testing n development
- `invoice Entity.drawid.pdf`: a simple ERD 

## Tables

### 1. Employees
- main table for employee info
- has personal/professional details
- each employee has unique id
- fields: name, contact, dept, position, salary, hire date

### 2. Users
- stores auth stuff
- 1:1 with Employees
- has login creds and role info
- uses BCrypt for pwd hashing
- has 2 roles: 'Employee' and 'InvoiceManager'

### 3. Contracts
- stores employee contract details
- has contract dates and payment rates
- supports diff contract types (FullTime, Contract, Freelance)
- includes pay grades (junior thru expert)
- links to Employees table

### 4. Invoices
- stores submitted invoices
- has invoice period, days worked, amount
- links to Employee and Contract
- includes status tracking (Draft, Submitted, Approved, Rejected)
- enforces biz rules via constraints

## Indexes

schema has indexes to make queries faster:
- FK indexes for joins
- unique indexes on usernames/emails
- composite indexes for date ranges
- status index for filtering invoices

## Constraints

schema enforces several biz rules:
- date validations (end after start)
- positive days worked
- valid contract types/grades
- valid invoice statuses
- unique employee-user relationships

## Sample Data

the `seed-data.sql`