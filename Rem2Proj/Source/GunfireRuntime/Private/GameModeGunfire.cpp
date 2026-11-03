#include "GameModeGunfire.h"
#include "GameRules.h"
#include "Templates/SubclassOf.h"

AGameModeGunfire::AGameModeGunfire(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->GameRulesClass = AGameRules::StaticClass();
    this->GameRules = NULL;
}


void AGameModeGunfire::OnPlayerDied_Implementation(APlayerController* Player) {
}

void AGameModeGunfire::ApplyDamage(AActor* CauseActor, AActor* TargetActor, float Damage, float DamageMod, float DamageScalar, TSubclassOf<UDamageTypeGunfire> DamageType, TSubclassOf<UImpactDirection> ImpactDirection) {
}


