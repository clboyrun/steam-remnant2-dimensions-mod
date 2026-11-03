#include "GunfireOnlineMatchmaker.h"

UGunfireOnlineMatchmaker::UGunfireOnlineMatchmaker() {
}

void UGunfireOnlineMatchmaker::SetMatchmakingRegion(EMatchmakingRegions Region) {
}

void UGunfireOnlineMatchmaker::Matchmake(ESessionSearchType NewSearchType, int32 MaxSessions, float Timeout) {
}

bool UGunfireOnlineMatchmaker::IsFindingSessions() const {
    return false;
}

EMatchmakingRegions UGunfireOnlineMatchmaker::GetMatchmakingRegion() const {
    return EMatchmakingRegions::UNKNOWN;
}

UGunfireOnlineMatchmaker* UGunfireOnlineMatchmaker::GetGunfireOnlineMatchmaker(const UObject* WorldContextObject) {
    return NULL;
}

EMatchmakingRegions UGunfireOnlineMatchmaker::DetectMatchmakingRegion() const {
    return EMatchmakingRegions::UNKNOWN;
}

void UGunfireOnlineMatchmaker::CancelFindSessions() {
}


