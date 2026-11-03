#include "HitPauseUtil.h"
#include "Templates/SubclassOf.h"

UHitPauseUtil::UHitPauseUtil() {
}

void UHitPauseUtil::ApplyHitPause(AActor* Actor, TSubclassOf<UDamageTypeGunfire> DamageTypeClass, float DurationScalar) {
}

bool UHitPauseUtil::AllowsHitPause(AActor* Actor, const FDamageInfo& DamageInfo, bool bCheckOwner) {
    return false;
}


