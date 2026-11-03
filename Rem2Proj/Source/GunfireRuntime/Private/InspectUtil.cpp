#include "InspectUtil.h"

UInspectUtil::UInspectUtil() {
}

void UInspectUtil::SetInspectStat(FInspectInfo& Info, FName NameID, const FInspectStat& Stat) {
}

bool UInspectUtil::GetInspectStatForMod(const FInspectMod& Mod, FName NameID, FInspectStat& OutStat) {
    return false;
}

bool UInspectUtil::GetInspectStat(const FInspectInfo& Info, FName NameID, FInspectStat& OutStat) {
    return false;
}

bool UInspectUtil::GetInspectMod(const FInspectInfo& Info, FName NameID, FInspectMod& OutMod) {
    return false;
}

FInspectInfo UInspectUtil::GetInspectBaseInfoForStat(UStatInfoSet* Stats, FName StatName) {
    return FInspectInfo{};
}

void UInspectUtil::ClearStats(FInspectInfo& Info) {
}


