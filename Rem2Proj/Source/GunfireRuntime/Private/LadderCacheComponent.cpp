#include "LadderCacheComponent.h"
#include "Net/UnrealNetwork.h"

ULadderCacheComponent::ULadderCacheComponent(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->RungSpacing = 30.00f;
    this->Link = NULL;
}

void ULadderCacheComponent::GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const {
    Super::GetLifetimeReplicatedProps(OutLifetimeProps);
    
    DOREPLIFETIME(ULadderCacheComponent, InternalOccupantCache);
}


