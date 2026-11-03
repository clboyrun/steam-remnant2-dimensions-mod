#include "WeaponComponent.h"

UWeaponComponent::UWeaponComponent(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
}

void UWeaponComponent::NotifyHitTarget_Implementation(const FDamageInfo& DamageInfo) {
}

void UWeaponComponent::ModifyInspectInfo(AActor* Actor, const FInspectInfo& In, FInspectInfo& Out) {
}

void UWeaponComponent::ModifyDamage_Implementation(FDamageInfo& Info) {
}

void UWeaponComponent::ComputeStats_Implementation(UStatsComponent* Stats) {
}


