#include "AICoordinatorBase.h"

UAICoordinatorBase::UAICoordinatorBase(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->DefaultCombatClass = NULL;
    this->PlayerCombatClass = NULL;
}

bool UAICoordinatorBase::IsTargeted(AActor* Actor) const {
    return false;
}

bool UAICoordinatorBase::IsCloselyTargeted(AActor* Actor) const {
    return false;
}

UAICoordinatorBase* UAICoordinatorBase::GetAICoordinator(UObject* WorldContextObject) {
    return NULL;
}


