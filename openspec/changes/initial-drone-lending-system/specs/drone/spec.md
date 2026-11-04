# Spec Delta

## Purpose

Lets the teacher and students register the school's drones and see at a glance which drones exist and whether each one is currently available or loaned.

## ADDED Requirements

### Requirement: Create Drone
The system SHALL allow a drone to be created with a name and a free-text description (REQ-001). The name MUST NOT be empty and MUST be unique among drones. The description MAY be empty. The system SHALL assign each drone an identifier. A drone's status SHALL NOT be accepted as input.

#### Scenario: Create a drone with name and description
- **GIVEN** no drone named "TELLO-07" exists
- **WHEN** a drone is created with name "TELLO-07" and description "SSID: TELLO-9FA21B, rechter Propellerschutz beschädigt"
- **THEN** the drone is stored with an identifier, that name and that description
- **AND** the drone's status is "Available"

#### Scenario: Reject a drone without a name
- **WHEN** a drone is created with an empty name
- **THEN** the creation is rejected with a validation error
- **AND** no drone is stored

#### Scenario: Reject a duplicate drone name
- **GIVEN** a drone named "TELLO-07" exists
- **WHEN** another drone is created with name "TELLO-07"
- **THEN** the creation is rejected
- **AND** only one drone named "TELLO-07" exists

### Requirement: List Drones
The system SHALL list all drones with their name, status and description (REQ-002). The status SHALL be either "Available" or "Loaned".

#### Scenario: List drones
- **GIVEN** the drones "TELLO-02" and "TELLO-07" exist
- **WHEN** the drone list is requested
- **THEN** both drones are listed with name, status and description

#### Scenario: Empty drone list
- **GIVEN** no drones exist
- **WHEN** the drone list is requested
- **THEN** an empty list is returned

### Requirement: Derived Drone Status
A drone's status SHALL be derived from its loans: "Loaned" when the drone has an active loan, otherwise "Available". The status SHALL NOT be set directly.

#### Scenario: Drone with an active loan is loaned
- **GIVEN** "TELLO-07" has an active loan
- **WHEN** the drone list is requested
- **THEN** "TELLO-07" is listed with status "Loaned"

#### Scenario: Drone with only returned loans is available
- **GIVEN** "TELLO-07" was loaned and the loan has been returned
- **WHEN** the drone list is requested
- **THEN** "TELLO-07" is listed with status "Available"
