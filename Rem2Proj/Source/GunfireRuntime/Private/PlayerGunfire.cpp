#include "PlayerGunfire.h"
#include "InputStateComponent.h"

APlayerGunfire::APlayerGunfire(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->InputStateComponent = CreateDefaultSubobject<UInputStateComponent>(TEXT("InputStateComponent"));
}

bool APlayerGunfire::IsPlayerTargeted() {
    return false;
}


