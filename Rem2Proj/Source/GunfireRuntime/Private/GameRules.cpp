#include "GameRules.h"

AGameRules::AGameRules(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->bAlwaysRelevant = true;
    this->bNetLoadOnClient = false;
    this->bReplicates = true;
    const FProperty* p_RemoteRole = GetClass()->FindPropertyByName("RemoteRole");
    (*p_RemoteRole->ContainerPtrToValuePtr<TEnumAsByte<ENetRole>>(this)) = ROLE_SimulatedProxy;
    this->NetDormancy = DORM_Initial;
}

float AGameRules::GetWeaponDamage(AActor* ItemOwner, AWeaponBase* Item, bool ApplyModifiers) {
    return 0.0f;
}

FDamageInfo AGameRules::GetDamageInfo(AActor* ItemOwner, AWeaponBase* Item) {
    return FDamageInfo{};
}


