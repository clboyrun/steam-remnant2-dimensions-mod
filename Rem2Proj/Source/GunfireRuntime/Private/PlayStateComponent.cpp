#include "PlayStateComponent.h"

UPlayStateComponent::UPlayStateComponent(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->StateSet = NULL;
    this->CurrentState = NULL;
}

bool UPlayStateComponent::IsLocallyControlled() const {
    return false;
}

bool UPlayStateComponent::IsHidingScreen() const {
    return false;
}

APlayerControllerGunfire* UPlayStateComponent::GetOwningPlayerController() const {
    return NULL;
}

float UPlayStateComponent::GetElapsedTransitionTime() const {
    return 0.0f;
}

UPlayStateEffect* UPlayStateComponent::GetActiveEffect(UClass* Class) const {
    return NULL;
}


