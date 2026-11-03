#include "PlayerStateRemnant.h"
#include "Net/UnrealNetwork.h"

APlayerStateRemnant::APlayerStateRemnant(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->CrossplayPlatform = 0;
    this->bTraveling = false;
    this->bIsLocalPlayerTarget = false;
}

void APlayerStateRemnant::SetIsLocalPlayerTarget(bool IsTarget) {
}

void APlayerStateRemnant::OnRep_Traveling() {
}

void APlayerStateRemnant::OnRep_CrossplayPlatform() {
}

bool APlayerStateRemnant::IsTraveling() const {
    return false;
}

void APlayerStateRemnant::GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const {
    Super::GetLifetimeReplicatedProps(OutLifetimeProps);
    
    DOREPLIFETIME(APlayerStateRemnant, CrossplayPlatform);
    DOREPLIFETIME(APlayerStateRemnant, bTraveling);
}


