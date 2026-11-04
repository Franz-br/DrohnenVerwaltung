# Spec Delta

## Purpose

Records which student group has loaned which drone with how many batteries and propellers, when it was loaned and returned, and keeps every loan as permanent history.

## ADDED Requirements

### Requirement: Loan Drone
An available drone SHALL be loaned to an existing student group (REQ-004). Creating a loan SHALL record the drone, the student group, the loan date (the current date and time) and the status "Active". The loan date SHALL NOT be accepted as input.

#### Scenario: Loan an available drone
- **GIVEN** the drone "TELLO-07" is available
- **AND** the student group "Group 3" exists
- **WHEN** "TELLO-07" is loaned to "Group 3"
- **THEN** an active loan is created for "TELLO-07" and "Group 3" with today's loan date
- **AND** "TELLO-07" has status "Loaned"

#### Scenario: Reject a loan for an unknown drone
- **GIVEN** no drone with the given identifier exists
- **WHEN** a loan is created for that drone
- **THEN** the loan is rejected as not found
- **AND** no loan is stored

#### Scenario: Reject a loan for an unknown student group
- **GIVEN** the drone "TELLO-07" is available
- **AND** no student group with the given identifier exists
- **WHEN** "TELLO-07" is loaned to that student group
- **THEN** the loan is rejected as not found
- **AND** "TELLO-07" remains available

### Requirement: No Double Loan
A drone SHALL have at most one active loan (REQ-005). A loan for a drone that already has an active loan SHALL be rejected, regardless of which student group requests it.

#### Scenario: Reject a second loan of a loaned drone
- **GIVEN** "TELLO-07" already has an active loan for "Group 3"
- **WHEN** "Group 1" tries to loan "TELLO-07"
- **THEN** the loan is rejected as a conflict
- **AND** "TELLO-07" still has exactly one active loan, for "Group 3"

#### Scenario: Loan again after return
- **GIVEN** "TELLO-07" was loaned to "Group 3" and the loan has been returned
- **WHEN** "Group 1" loans "TELLO-07"
- **THEN** a new active loan is created for "Group 1"

### Requirement: Loan Accessories
A loan SHALL record accessories as loan items, each with a type and a quantity (REQ-006). The allowed types SHALL be "Battery" and "Propeller". Quantities SHALL be integers greater than or equal to 0. Each type SHALL appear at most once per loan; a type that is not given SHALL count as quantity 0.

#### Scenario: Loan a drone with batteries and propellers
- **GIVEN** "TELLO-07" is available and "Group 3" exists
- **WHEN** "TELLO-07" is loaned to "Group 3" with 4 batteries and 8 propellers
- **THEN** the active loan records 4 batteries and 8 propellers

#### Scenario: Loan a drone without accessories
- **WHEN** an available drone is loaned to a group without accessories
- **THEN** the active loan records 0 batteries and 0 propellers

#### Scenario: Reject a negative quantity
- **WHEN** an available drone is loaned with -1 batteries
- **THEN** the loan is rejected with a validation error
- **AND** no loan is stored and the drone remains available

#### Scenario: Reject an unknown accessory type
- **WHEN** an available drone is loaned with an accessory of type "Charger"
- **THEN** the loan is rejected with a validation error
- **AND** no loan is stored

#### Scenario: Reject a duplicate accessory type
- **WHEN** an available drone is loaned with two "Battery" items
- **THEN** the loan is rejected with a validation error
- **AND** no loan is stored

### Requirement: List Active Loans
The system SHALL list all active loans with student group, drone, battery quantity, propeller quantity and loan date (REQ-007). Returned loans SHALL NOT appear in this list.

#### Scenario: List active loans
- **GIVEN** "TELLO-02" is loaned to "Group 1" with 3 batteries and 4 propellers on 19.08.2026
- **AND** "TELLO-07" is loaned to "Group 3" with 4 batteries and 8 propellers on 19.08.2026
- **WHEN** the active loans are requested
- **THEN** both loans are listed with group, drone, batteries, propellers and loan date

#### Scenario: Returned loan is not active
- **GIVEN** the loan of "TELLO-07" to "Group 3" has been returned
- **WHEN** the active loans are requested
- **THEN** that loan is not listed

### Requirement: Return Loan
An active loan SHALL be returnable (REQ-008). Returning SHALL change the loan's status from "Active" to "Returned", after which the drone SHALL be available again. Returning a loan that is already returned SHALL be rejected. An unknown loan SHALL be rejected as not found.

#### Scenario: Return a loan
- **GIVEN** "TELLO-07" is loaned to "Group 3"
- **WHEN** the loan is returned
- **THEN** the loan is marked as returned
- **AND** "TELLO-07" becomes available again

#### Scenario: Reject returning a returned loan
- **GIVEN** the loan of "TELLO-07" to "Group 3" has already been returned
- **WHEN** the loan is returned again
- **THEN** the return is rejected as a conflict
- **AND** the original return date is unchanged

### Requirement: Record Return Date
Returning a loan SHALL record the return date as the current date and time (REQ-009). An active loan SHALL have no return date. The return date SHALL NOT be accepted as input.

#### Scenario: Return date is stored
- **GIVEN** "TELLO-07" is loaned to "Group 3" and the loan has no return date
- **WHEN** the loan is returned
- **THEN** the loan's return date is set to the time of the return

### Requirement: Keep Loan History
Returned loans SHALL be kept permanently and SHALL NOT be deleted (REQ-010). A return SHALL change only the loan's status and return date; drone, student group, loan date and accessory quantities SHALL remain unchanged. The system SHALL list returned loans as loan history with student group, drone, battery quantity, propeller quantity, loan date and return date.

#### Scenario: Returned loan appears in history unchanged
- **GIVEN** "TELLO-07" was loaned to "Group 3" with 4 batteries and 8 propellers on 19.08.2026
- **WHEN** the loan is returned and the loan history is requested
- **THEN** the history lists the loan with "Group 3", "TELLO-07", 4 batteries, 8 propellers, loan date 19.08.2026 and its return date

#### Scenario: History keeps several loans of the same drone
- **GIVEN** "TELLO-07" was loaned and returned twice
- **WHEN** the loan history is requested
- **THEN** both returned loans of "TELLO-07" are listed
