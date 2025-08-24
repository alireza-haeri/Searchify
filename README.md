# **Searchify**  
> A modern, high-performance **Book Search API** powered by **.NET 9**, **Elasticsearch 9**, and **Docker**.  
Built to demonstrate **real-world search scenarios**, optimized queries, and a production-ready backend setup.

<br>

![.NET](https://img.shields.io/badge/.NET-9.0-blue)
![Elastic](https://img.shields.io/badge/Elasticsearch-9.x-yellow)
![Docker](https://img.shields.io/badge/Docker-ready-blue)
![Status](https://img.shields.io/badge/Status-Active-brightgreen)

<br>

## **✨ Features**
- **Full-text search** across title, author, description, categories, and publisher fields.  
- **Smart suggestions** powered by Elastic's `completion` suggesters.  
- **Advanced filtering & sorting** for precise search results.  
- **Top books & analytics endpoints** for curated insights.  
- **Containerized setup** with `docker-compose` for Elasticsearch + API.  

<br>

## **🚀 Quick Start**

### **1. Clone the repository**
    git clone https://github.com/alireza-haeri/Searchify.git
    cd Searchify

### **2. Start Elasticsearch & Kibana**
You need **Elasticsearch 9.x**.  
Pull it from [Docker Hub](https://hub.docker.com/_/elasticsearch):

    docker compose up -d

### **3. Run the API**
    dotnet build
    dotnet run --project src/Searchify.Api

<br>

## **📡 API Highlights**

| Method | Endpoint | Description |
|---------|----------|-------------|
| ![GET](https://img.shields.io/badge/GET-blue) | `/api/books/search` | Advanced multi-field search with filters, pagination, and sorting |
| ![GET](https://img.shields.io/badge/GET-blue) | `/api/books/suggestion` | Smart type-ahead suggestions |
| ![GET](https://img.shields.io/badge/GET-blue) | `/api/books/topbooks` | Get top-rated books, with optional category filtering |
| ![GET](https://img.shields.io/badge/GET-blue) | `/api/books/categories` | Aggregated stats on all categories |
| ![GET](https://img.shields.io/badge/GET-blue) | `/api/books/publishers` | Insights on publishers and their average ratings |
| ![GET](https://img.shields.io/badge/GET-blue) | `/api/book/{isbn}` | Get a specific book by ISBN |
| ![POST](https://img.shields.io/badge/POST-green) | `/api/book` | Add a new book |
| ![PUT](https://img.shields.io/badge/PUT-orange) | `/api/book/{isbn}` | Update book info |
| ![DELETE](https://img.shields.io/badge/DELETE-red) | `/api/book/{isbn}` | Delete a book |


### **🔍 Search Example**
    GET /api/books/search
    ?title=elastic
    &categories=Database
    &page=1
    &pageSize=10
    &sortBy=rating
    &sortOrder=desc


### **💡 Suggestion Example**
    GET /api/books/suggestion?q=learn

**Response**
```
    [
      { "title": "Learning Elasticsearch", "author": "John Doe", "rating": 4.7 },
      { "title": "Learning .NET 9", "author": "Jane Smith", "rating": 4.5 }
    ]
```

<br>

## **🛠 Built With**
- **.NET 9 Minimal APIs** — clean, lightweight endpoints  
- **Elasticsearch 9** — advanced search, aggregations, and suggestions  
- **Docker & Docker Compose** — seamless local setup for API + Elastic stack  
- **FluentValidation** — clean input validation


<br>

### 📫 Let's Connect!

Feel free to reach out if you want to collaborate on a project or just want to chat.

- 🔗 **LinkedIn:** [in/alireza-haeri-dev](https://www.linkedin.com/in/alireza-haeri-dev)  
- 🔗 **Telegram:** [@AlirezaHaeriDev](https://t.me/AlirezaHaeriDev)  
- 🔗 **Email:** alireza.haeri.dev@gmail.com  

