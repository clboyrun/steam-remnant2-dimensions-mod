#include "StateMachine.h"

UStateMachine::UStateMachine() {
    this->DefaultState = TEXT("Default");
}

UActorState* UStateMachine::GetStateParent(UActorState* State) const {
    return NULL;
}

UActorState* UStateMachine::GetState(const FName& NameID) const {
    return NULL;
}

UActorState* UStateMachine::GetRootState(UActorState* State) const {
    return NULL;
}


