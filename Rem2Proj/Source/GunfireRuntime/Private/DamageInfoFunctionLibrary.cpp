#include "DamageInfoFunctionLibrary.h"

UDamageInfoFunctionLibrary::UDamageInfoFunctionLibrary() {
}

FDamageInfo UDamageInfoFunctionLibrary::Conv_RepInfoToDamageInfo(const FReplicatedDamageInfo& InRepInfo) {
    return FDamageInfo{};
}

FReplicatedDamageInfo UDamageInfoFunctionLibrary::Conv_DamageInfoToRepInfo(const FDamageInfo& InInfo) {
    return FReplicatedDamageInfo{};
}


