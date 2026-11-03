#include "AICharacter.h"
#include "AIControllerGunfire.h"
#include "EDecalReceiverType.h"
#include "Net/UnrealNetwork.h"

AAICharacter::AAICharacter(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->AIControllerClass = AAIControllerGunfire::StaticClass();
    this->ReceiveDecals = EDecalReceiverType::Disabled;
    this->Blackboard = NULL;
    this->SpawnPath = NULL;
    this->PatrolPath = NULL;
    this->PerceptionScalar = 1.00f;
    this->CombatClass = NULL;
    this->NoDamageWhenNotPlayerVisible = false;
    this->NoDamageWhenNotPlayerDamageScalar = 0.01f;
    this->NoDamageWhenNotPlayerVisibleMaxRange = 2000.00f;
}

void AAICharacter::OnRep_CurrentPOIAnimTags() {
}

void AAICharacter::DifficultyChanged() {
}

void AAICharacter::GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const {
    Super::GetLifetimeReplicatedProps(OutLifetimeProps);
    
    DOREPLIFETIME(AAICharacter, CurrentPOIAnimationTags);
}


