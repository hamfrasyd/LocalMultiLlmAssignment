### API Usage

#### Endpoints
- POST /api/todos: Create a todo with title constraints
- GET /api/todos/{id}: Retrieve a todo by ID

#### Status Codes
- 201 Created: Successful creation
- 400 Bad Request: Invalid title
- 404 Not Found: Non-existent todo

#### Example
POST /api/todos

Body:
{
  "title": "Write assignment"
}

Response:
201 Created
{
  "id": 1,
  "title": "Write assignment",
  "isCompleted": false
}