#include "BlueprintStateDecorator.h"

UBlueprintStateDecorator::UBlueprintStateDecorator() {
}

bool UBlueprintStateDecorator::OnValidate_Implementation(UStateMachineComponent* StateMachine, AActor* Actor) const {
    return false;
}




