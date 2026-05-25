# Web-Based Internship Management System

A web-based internship management platform developed with ASP.NET Core MVC.  
The system allows students to submit internship applications, select or add companies, upload required documents, keep internship diaries, and communicate with academic advisors through approval, revision, and comment workflows.

## 📌 Project Overview

This project is designed to digitalize and manage the internship application process in universities.  
It provides separate panels for students and academic advisors, enabling a structured workflow from internship application to final approval.

The system supports:

- Student internship applications
- Company selection and company information management
- Turkey map-based company/internship visualization
- Academic pre-approval and final approval workflow
- Internship contract/form generation after pre-approval
- Document upload and advisor submission process
- Internship diary creation with rich text editor
- Academic review, comments, revisions, and approval for diary entries

## 🚀 Features

### Student Features

- Register and log in as a student
- Create internship applications
- Select an existing company or add a new company
- Choose company city for map-based listing
- View internship application status
- Download internship contract/form after academic pre-approval
- Upload signed internship documents
- Send uploaded documents to academic advisor
- Create daily internship diary entries
- Use rich text editor for diary content
- Add images to diary entries
- Edit diary entries only when allowed
- Submit diary entries for academic review
- View academic comments and revision requests

### Academic Advisor Features

- View pending internship applications
- Approve or reject internship applications
- Review uploaded internship contract/form
- Approve internship documents
- View submitted diary entries
- Filter diary entries by student
- Add comments to diary entries
- Request revisions from students
- Approve internship diary entries

### Map and Company Features

- Turkey map integration with city boundaries
- City-based company listing
- Shows companies with accepted internship history
- Company details can be auto-filled from existing records
- Students can select a company from the map sidebar
- Company suggestions appear while typing company name

## 🛠️ Technologies Used

- ASP.NET Core MVC
- Entity Framework Core
- SQL Server
- Razor Views
- Bootstrap
- JavaScript
- Leaflet.js
- Quill Rich Text Editor
- Flatpickr Date Picker
- HTML / CSS

## 🧩 Main Modules

```text
Controllers/
 ├── StudentController
 ├── InternshipController
 ├── InternshipDiaryController
 ├── AcademicController
 └── AdminController

Models/
 ├── Student
 ├── Academic
 ├── Internship
 ├── Company
 ├── InternshipDiary
 ├── InternshipFile
 ├── DiaryFile
 ├── Comment
 ├── City
 ├── Country
 └── Department

Views/
 ├── Student
 ├── Internship
 ├── InternshipDiary
 ├── Academic
 └── Shared