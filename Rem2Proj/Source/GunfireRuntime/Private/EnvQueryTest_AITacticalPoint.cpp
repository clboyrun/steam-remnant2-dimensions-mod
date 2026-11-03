#include "EnvQueryTest_AITacticalPoint.h"

UEnvQueryTest_AITacticalPoint::UEnvQueryTest_AITacticalPoint() {
    this->TestPurpose = EEnvTestPurpose::Filter;
    this->FilterType = EEnvTestFilterType::Match;
    this->ScoringEquation = EEnvTestScoreEquation::Constant;
}


