#include "CursorResponseManager.h"

UCursorResponseManager::UCursorResponseManager(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->CachedController = NULL;
}

void UCursorResponseManager::SetClickBehavior(ECursorInteractBehavior Behavior) {
}

void UCursorResponseManager::SetActiveState(bool bShouldBeActive) {
}

UCursorResponseManager* UCursorResponseManager::GetInstance(APlayerController* Controller) {
    return NULL;
}

bool UCursorResponseManager::GetActiveState() {
    return false;
}


