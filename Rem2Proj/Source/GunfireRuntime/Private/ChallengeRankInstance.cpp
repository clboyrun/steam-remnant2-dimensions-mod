#include "ChallengeRankInstance.h"

UChallengeRankInstance::UChallengeRankInstance() {
    this->ChallengeInstance = NULL;
    this->Rank = NULL;
    this->RankId = 0;
}

bool UChallengeRankInstance::IsComplete() const {
    return false;
}

TArray<UChallengeObjectiveBase*> UChallengeRankInstance::GetObjectives() const {
    return TArray<UChallengeObjectiveBase*>();
}

TArray<FChallengeObjectiveInstance> UChallengeRankInstance::GetObjectiveInstances() const {
    return TArray<FChallengeObjectiveInstance>();
}

FChallengeObjectiveInstance UChallengeRankInstance::GetObjectiveInstance(UChallengeObjectiveBase* Objective) const {
    return FChallengeObjectiveInstance{};
}


