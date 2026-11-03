#include "SwimmingCondition.h"

USwimmingCondition::USwimmingCondition() {
    this->Type = ESwimmingType::Surface;
    this->Method = ESwimmingStateMethod::Calculated;
}

ESwimmingType USwimmingCondition::GetSwimmingState(UStateMachineComponent* StateMachine) {
    return ESwimmingType::Surface;
}


