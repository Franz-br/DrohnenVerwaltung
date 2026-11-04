# Spec Delta

## Purpose

Lets the teacher register the student groups that borrow drones, so that every loan names a responsible group.

## ADDED Requirements

### Requirement: Create Student Group
The system SHALL allow a student group to be created with a name (REQ-003). The name MUST NOT be empty and MUST be unique among student groups. The system SHALL assign each student group an identifier.

#### Scenario: Create a student group
- **GIVEN** no student group named "Group 3" exists
- **WHEN** a student group is created with name "Group 3"
- **THEN** the student group is stored with an identifier and the name "Group 3"

#### Scenario: Reject a student group without a name
- **WHEN** a student group is created with an empty name
- **THEN** the creation is rejected with a validation error
- **AND** no student group is stored

#### Scenario: Reject a duplicate student group name
- **GIVEN** a student group named "Group 3" exists
- **WHEN** another student group is created with name "Group 3"
- **THEN** the creation is rejected
- **AND** only one student group named "Group 3" exists

### Requirement: List Student Groups
The system SHALL list all student groups with their name, so that a group can be selected when a drone is loaned.

#### Scenario: List student groups
- **GIVEN** the student groups "Group 1" and "Group 3" exist
- **WHEN** the student group list is requested
- **THEN** both student groups are listed with their name
