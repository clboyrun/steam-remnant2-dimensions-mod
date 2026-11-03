#include "UtilityItemInstanceData.h"
#include "Net/UnrealNetwork.h"

UUtilityItemInstanceData::UUtilityItemInstanceData() {
    this->Charges = 0;
    this->Cooldown = 0.00f;
}

void UUtilityItemInstanceData::GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const {
    Super::GetLifetimeReplicatedProps(OutLifetimeProps);
    
    DOREPLIFETIME(UUtilityItemInstanceData, Charges);
    DOREPLIFETIME(UUtilityItemInstanceData, Cooldown);
}


