#include "RemnantPingManager.h"

URemnantPingManager::URemnantPingManager(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->PrimaryAmmoRequest = NULL;
    this->SecondaryAmmoRequest = NULL;
    this->HealingRequest = NULL;
    this->RangedWeaponType = NULL;
    this->PrimaryAmmoClass = NULL;
    this->SecondaryAmmoClass = NULL;
    this->PingActorBP = NULL;
    this->DefaultPingDuration = 3.00f;
    this->VoCooldownDuration = 1.00f;
    this->AllowedPingsPerInterval = 4;
    this->PingCooldownDuration = 5.00f;
}

bool URemnantPingManager::IsCharacterOnVOCooldown(AActor* Character) {
    return false;
}

bool URemnantPingManager::IsCharacterOnPingCooldown(AActor* Character) {
    return false;
}

float URemnantPingManager::GetRemainingCharacterPingCooldown(AActor* Character) {
    return 0.0f;
}

URemnantPingManager* URemnantPingManager::GetPingManager(const UObject* WorldContextObject) {
    return NULL;
}

bool URemnantPingManager::GetPingFromCharacter(AActor* Character, FActionPing& OutPing, int32& PingIndex) {
    return false;
}

TArray<FActionPing> URemnantPingManager::GetCurrentPings() {
    return TArray<FActionPing>();
}

void URemnantPingManager::Client_UpdateCharacterPing_Implementation(AActor* Character, const FActionPing& NewPing) {
}

void URemnantPingManager::Client_MarkPlayerSoloPing_Implementation(AActor* Character, const bool IsSolo) {
}


