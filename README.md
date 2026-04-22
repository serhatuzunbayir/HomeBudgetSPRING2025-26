# HomeBudgetSPRING2025-26

## Project Overview

Home Budget is a budget and expense tracking application designed for both individual users and families. The project includes desktop and web applications that work with the same shared data so users can manage expenses, budgets, and financial goals across both platforms. It supports day-to-day expense logging, category-based budgeting, budget alerts, and spending analytics. The system also helps families monitor shared spending by allowing visibility into member expenses and family-level budget summaries.

## Requirements

### Functional Requirements

- **FR1 - User Accounts and Family Membership**
  - **Description:** Users can register with a name and password, and log in to access the system. Each user can belong to only one family at a time. All account data is saved to a central database shared between the desktop and web apps.
  - **Priority:** High
- **FR2 - Personal Expense Logging**
  - **Description:** Users can add, view, and delete their own expenses at any time, even without being in a family. Each expense must have an amount, category, date, and description. Changes are saved to the shared database and reflected across both apps immediately.
  - **Priority:** High
- **FR3 - Family Creation and Invitations**
  - **Description:** Each family has at least one admin. The admin can send invitations to other users. The admin can also remove members from the family at any time.
  - **Priority:** High
- **FR4 - Category Budgets**
  - **Description:** Users can add or remove spending budgets per category (e.g. Food, Transport). When a new expense is logged, the system automatically deducts the amount from the matching category's remaining budget using a data query.
  - **Priority:** High
- **FR5 - Budget Alert Notifications**
  - **Description:** Users can set a warning threshold for each category budget. When the remaining budget falls below that amount, the system fires an alert and shows a notification to the user on both the desktop and web app.
  - **Priority:** High
- **FR6 - Expense Analytics Per User**
  - **Description:** On the web app, users can view a summary of their spending: total expenses and a monthly average broken down by category. These figures are calculated by querying and grouping the stored expenses in real time.
  - **Priority:** Medium
- **FR7 - Family Expense Visibility**
  - **Description:** Every member of a family can view all expenses logged by any other member of the same family. The list can be filtered by member, category, or date range using data queries on the shared database.
  - **Priority:** Medium
- **FR8 - Spending Goals**
  - **Description:** Users can set a personal saving or spending goal with a target amount and deadline. The system tracks progress automatically by comparing the goal against logged expenses, and fires an event when the goal is reached or missed.
  - **Priority:** Medium
- **FR9 - Expense Filtering and Sorting**
  - **Description:** Users can filter their expense list by date range, category, or amount, and sort results in ascending or descending order. The desktop app provides input fields and dropdowns in the Windows Forms interface to control the view.
  - **Priority:** Low
- **FR10 - Family Budget Overview**
  - **Description:** Family admins can view a combined budget summary for the whole family, showing total spending per category across all members. The web app displays this as a dashboard page with running totals fetched through the API.
  - **Priority:** Low

### Non-Functional Requirements

- **NF1 - Single Shared Data Store**
  - **Description:** Both the desktop and web applications must read from and write to the same central database (SQL Server or SQLite). No data should be stored only on one side all records must be visible to both apps without manual sync.
  - **Priority:** High
- **NF2 - Desktop App with a Proper Windows Interface**
  - **Description:** The desktop application must be built using MAUI. It must provide clear screens for logging expenses, managing budgets, and viewing summaries. All forms should be clean, labelled properly, and easy to use without extra instructions.
  - **Priority:** High
- **NF3 - Web App Built on a Clear MVC Structure**
  - **Description:** The web application must separate data models, page views, and business logic into distinct layers. Models hold the data, controllers process requests and talk to the database, and views display results to the user.
  - **Priority:** High
- **NF4 - Web API as the Communication Layer**
  - **Description:** All data exchange between the desktop app and the web must go through a RESTful Web API. The API must handle standard operations: creating, reading, updating, and deleting records. Both apps must use the same API endpoints to keep data consistent.
  - **Priority:** High
- **NF5 - Responsive and Fast User Experience**
  - **Description:** The system should respond to any user action adding an expense, loading a list, or triggering a budget alert within 2 seconds under normal conditions. Budget alert notifications must appear immediately after the triggering expense is saved, without requiring a page refresh.
  - **Priority:** High
