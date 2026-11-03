#include "FactionManager.h"
#include "Templates/SubclassOf.h"

UFactionManager::UFactionManager(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->FactionOverridePropogationRadius = 5000.00f;
}

void UFactionManager::PropagateFactionOverrideAtLocation(const FVector& Location, TSubclassOf<UFaction> Faction, TSubclassOf<UFaction> FactionToOverride, EAffiliation NewAffiliation, float PropogationRadiusOverride) {
}

void UFactionManager::PropagateFactionOverride(UFactionComponent* Source, TSubclassOf<UFaction> Faction, EAffiliation Affiliation, float PropogationRadiusOverride) {
}

UFactionManager* UFactionManager::GetFactionManager(UObject* WorldContextObject) {
    return NULL;
}


