#include "Archetype.h"
#include "EActorModifierScope.h"

UArchetype::UArchetype() {
    this->Scope = EActorModifierScope::ServerClient;
    this->RequiredExperience = 500.00f;
    this->RequiredExperienceInc = 1000.00f;
    this->RequiredExperienceExp = 1.20f;
    this->RequiredExperienceRound = 100;
}

void UArchetype::OnExperience(int32 Experience) {
}

void UArchetype::ModifyIncomingExperience_Implementation(int32& Experience) {
}

int32 UArchetype::GetRequiredExperienceForLevel_Implementation(AActor* Actor, int32 TargetLevel) {
    return 0;
}


