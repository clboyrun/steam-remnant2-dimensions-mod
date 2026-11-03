#include "UIHudDamageNumberComponent.h"

UUIHudDamageNumberComponent::UUIHudDamageNumberComponent(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->bInterpolateColorsOnOverlap = false;
    this->bUseCriticalParameters = false;
    this->bUseWeakSpotParameters = false;
    this->bUseCriticalWeakSpotParameters = false;
    this->bUseZeroDamageText = false;
    this->bUseRoundedDamageAsZeroDamage = true;
    this->bIgnoreSpecialsOnZeroDamage = true;
    this->ResistColorAlpha = 0.50f;
    this->CachedCharacter = NULL;
    this->CachedController = NULL;
}

void UUIHudDamageNumberComponent::OnNotifyHitTarget(const FDamageInfo& DamageInfo) {
}

void UUIHudDamageNumberComponent::ClearNumberQueue() {
}


