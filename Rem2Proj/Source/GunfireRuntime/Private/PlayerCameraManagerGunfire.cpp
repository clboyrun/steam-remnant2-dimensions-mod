#include "PlayerCameraManagerGunfire.h"
#include "Templates/SubclassOf.h"

APlayerCameraManagerGunfire::APlayerCameraManagerGunfire(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->DefaultModifiers.AddDefaulted(1);
    this->CurrentEffectID = 0;
}

void APlayerCameraManagerGunfire::SettingsRebuilt(UGameUserSettingsGunfire* UserSettings) {
}

void APlayerCameraManagerGunfire::RemoveCameraEffect(int32 HandleID) {
}

void APlayerCameraManagerGunfire::ModifyCameraShakeScale(TSubclassOf<UCameraShakeBase> ShakeClass, float Scale) {
}

void APlayerCameraManagerGunfire::ModifyCameraEffectIntensity(int32 HandleID, FName ParticleIntensityParam, float ParticleIntensity, bool ResetSystem) {
}

bool APlayerCameraManagerGunfire::GetCameraEffectForHandleID(int32 HandleID, FCameraEffect& OutEffect) {
    return false;
}

int32 APlayerCameraManagerGunfire::AddCameraEffect(FCameraEffect Effect, AActor* EffectViewTarget) {
    return 0;
}


