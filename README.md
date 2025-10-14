# CommonMorph
CommonMorph is a collaborative platform for documenting and expanding morphological resources, especially for low-resource and endangered languages.

## Overview
The repository includes two main components:
1. **.NET Web Application**:
 - Provides the main user interface for data collection, annotation, API calls, and rule-based morphology design.
 - Manages authentication, datasets, and API connections.
2. **FastAPI Backend**:
 - Handles neural network training, model inference, and active learning suggestions.
 - Interfaces with the .NET web app through REST APIs.

These components can be deployed independently.
