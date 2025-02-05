#pragma once

#include "application/common_include.h"
#include "application/common.h"
#include "application/logger.h"
#include "domain/args_value.h"

struct ArgsParserCommand {
	std::shared_ptr<args::Group> group;

	std::vector<std::tuple<std::any, std::function<void()>>> arguments;

	std::function<int()> action = []() { return 0; };

	ArgsParserCommand(std::shared_ptr<args::Group> _group) :
		group(_group) { }

	void setAction(std::function<int()> _action) {
		action = _action;
	}

	void addFlag(ArgsValue<bool>& val, args::Matcher matcher, const std::string& description, args::Options options = {}) {
		std::shared_ptr<args::Flag> flag = std::make_shared<args::Flag>(*group.get(), matcher.GetLongOrAny().str(), description, std::forward<args::Matcher>(matcher), options);
		std::function<void()> func = [flag, &val]() {
			val.matched = flag->Matched();
			val.value = flag->Get();
			};
		arguments.emplace_back(std::tuple<std::any, std::function<void()>>(flag, func));
	}

	template <typename TValue>
	void addValueFlag(ArgsValue<TValue>& val, args::Matcher matcher, const std::string& description, const TValue& defaultValue, const std::vector<std::string>& choices, args::Options options) {
		std::shared_ptr<args::ValueFlag<TValue>> flag = std::make_shared<args::ValueFlag<TValue>>(*group.get(), matcher.GetLongOrAny().str(), description, std::forward<args::Matcher>(matcher), defaultValue, options);
		if (choices.size() > 0) {
			flag->HelpChoices(choices);
		}
		std::function<void()> func = [flag, &val]() {
			val.matched = flag->Matched();
			val.value = flag->Get();
			};
		arguments.emplace_back(std::tuple<std::any, std::function<void()>>(flag, func));
	}

	template <typename TValue>
	void addValueFlag(ArgsValue<TValue>& val, args::Matcher matcher, const std::string& description) {
		addValueFlag<TValue>(val, std::move(matcher), description, TValue(), {}, {});
	}

	template <typename TValue>
	void addValueFlag(ArgsValue<TValue>& val, args::Matcher matcher, const std::string& description, const TValue& defaultValue) {
		addValueFlag<TValue>(val, std::move(matcher), description, defaultValue, {}, {});
	}

	template <typename TValue>
	void addValueFlag(ArgsValue<TValue>& val, args::Matcher matcher, const std::string& description, const std::vector<std::string>& choices) {
		addValueFlag<TValue>(val, std::move(matcher), description, TValue(), choices, {});
	}

	template <typename TValue>
	void addValueFlag(ArgsValue<TValue>& val, args::Matcher matcher, const std::string& description, args::Options options) {
		addValueFlag<TValue>(val, std::move(matcher), description, TValue(), {}, options);
	}

	template <typename TValue>
	void addValueFlag(ArgsValue<TValue>& val, args::Matcher matcher, const std::string& description, const TValue& defaultValue, const std::vector<std::string>& choices) {
		addValueFlag<TValue>(val, std::move(matcher), description, defaultValue, choices, {});
	}

	template <typename TValue>
	void addValueFlagList(ArgsValue<std::vector<TValue>>& val, args::Matcher matcher, const std::string& description, const std::vector<TValue>& defaultValue, const std::vector<std::string>& choices, args::Options options) {
		std::shared_ptr<args::ValueFlagList<TValue>> flag = std::make_shared<args::ValueFlagList<TValue>>(*group.get(), matcher.GetLongOrAny().str(), description, std::forward<args::Matcher>(matcher), defaultValue, options);
		if (choices.size() > 0) {
			flag->HelpChoices(choices);
		}
		std::function<void()> func = [flag, &val]() {
			val.matched = flag->Matched();
			val.value = flag->Get();
			};
		arguments.emplace_back(std::tuple<std::any, std::function<void()>>(flag, func));
	}

	template <typename TValue>
	void addValueFlagList(ArgsValue<std::vector<TValue>>& val, args::Matcher matcher, const std::string& description) {
		addValueFlagList<TValue>(val, std::move(matcher), description, {}, {}, {});
	}

	template <typename TValue>
	void addValueFlagList(ArgsValue<std::vector<TValue>>& val, args::Matcher matcher, const std::string& description, const std::vector<TValue>& defaultValue) {
		addValueFlagList<TValue>(val, std::move(matcher), description, defaultValue, {}, {});
	}

	template <typename TValue>
	void addValueFlagList(ArgsValue<std::vector<TValue>>& val, args::Matcher matcher, const std::string& description, const std::vector<std::string>& choices) {
		addValueFlagList<TValue>(val, std::move(matcher), description, {}, choices, {});
	}

	template <typename TValue>
	void addValueFlagList(ArgsValue<std::vector<TValue>>& val, args::Matcher matcher, const std::string& description, args::Options options) {
		addValueFlagList<TValue>(val, std::move(matcher), description, {}, {}, options);
	}

	template <typename TValue>
	void addKeyValueFlagList(ArgsValue<std::unordered_map<std::string, TValue>>& val, args::Matcher matcher, const std::string& description, const std::unordered_map<std::string, TValue>& defaultValue, const std::vector<std::string>& choices, args::Options options) {
		std::vector<TValue> defaultValueNorm{};
		for (auto& defaultValPair : defaultValue) {
			defaultValueNorm.emplace_back(std::string(defaultValPair.first) + "=" + std::string(defaultValPair.second));
		}
		std::shared_ptr<args::ValueFlagList<std::string>> flag = std::make_shared<args::ValueFlagList<std::string>>(*group.get(), matcher.GetLongOrAny().str(), description, std::forward<args::Matcher>(matcher), defaultValueNorm, options);
		if (choices.size() > 0) {
			flag->HelpChoices(choices);
		}
		std::function<void()> func = [flag, &val, &matcher]() {
			std::unordered_map<std::string, TValue> keyValuePairs{};
			for (auto& setVal : flag->Get()) {
				std::vector<std::string> splittedSetVal = Common::splitStr(setVal, { "=" });
				if (splittedSetVal.size() != 2) {
					throw std::runtime_error("Invalid key-value pair args \"" + setVal + "\"");
				}
				TValue value{};
				try {
					args::ValueReader()("", splittedSetVal[1], value);
				}
				catch (...) {
					throw std::runtime_error("Invalid key-value pair args \"" + setVal + "\"");
				}
				keyValuePairs[splittedSetVal[0]] = value;
			}
			val.matched = flag->Matched();
			val.value = keyValuePairs;
			};
		arguments.emplace_back(std::tuple<std::any, std::function<void()>>(flag, func));
	}

	template <typename TValue>
	void addKeyValueFlagList(ArgsValue<std::unordered_map<std::string, TValue>>& val, args::Matcher matcher, const std::string& description) {
		addKeyValueFlagList<TValue>(val, std::move(matcher), description, {}, {}, {});
	}

	template <typename TValue>
	void addKeyValueFlagList(ArgsValue<std::unordered_map<std::string, TValue>>& val, args::Matcher matcher, const std::string& description, const std::unordered_map<std::string, TValue>& defaultValue) {
		addKeyValueFlagList<TValue>(val, std::move(matcher), description, defaultValue, {}, {});
	}

	template <typename TValue>
	void addKeyValueFlagList(ArgsValue<std::unordered_map<std::string, TValue>>& val, args::Matcher matcher, const std::string& description, const std::vector<std::string>& choices) {
		addKeyValueFlagList<TValue>(val, std::move(matcher), description, {}, choices, {});
	}

	template <typename TValue>
	void addKeyValueFlagList(ArgsValue<std::unordered_map<std::string, TValue>>& val, args::Matcher matcher, const std::string& description, args::Options options) {
		addKeyValueFlagList<TValue>(val, std::move(matcher), description, {}, {}, options);
	}

	template <typename TValue>
	void addPositionalValueFlag(ArgsValue<TValue>& val, std::string name, const std::string& description, const TValue& defaultValue, const std::vector<std::string>& choices, args::Options options) {
		std::shared_ptr<args::Positional<TValue>> flag = std::make_shared<args::Positional<TValue>>(*group.get(), name, description, defaultValue, options);
		if (choices.size() > 0) {
			flag->HelpChoices(choices);
		}
		std::function<void()> func = [flag, &val]() {
			val.matched = flag->Matched();
			val.value = flag->Get();
			};
		arguments.emplace_back(std::tuple<std::any, std::function<void()>>(flag, func));
	}

	template <typename TValue>
	void addPositionalValueFlag(ArgsValue<TValue>& val, std::string name, const std::string& description) {
		addPositionalValueFlag<TValue>(val, name, description, TValue(), {}, {});
	}

	template <typename TValue>
	void addPositionalValueFlag(ArgsValue<TValue>& val, std::string name, const std::string& description, args::Options options) {
		addPositionalValueFlag<TValue>(val, name, description, TValue(), {}, options);
	}

	int parse() {
		for (auto& parser : arguments) {
			std::function<void()> func = std::get<1>(parser);
			func();
		}
		return action();
	}
};

struct ArgsParser : ArgsParserCommand {
	std::shared_ptr<args::ArgumentParser> program;

	std::shared_ptr<args::Group> commandGroup;

	std::vector<std::shared_ptr<ArgsParserCommand>> commandGroupArguments;

	std::shared_ptr<args::HelpFlag> helpFlag;

	ArgsParser(std::shared_ptr<args::ArgumentParser> _program) :
		ArgsParserCommand(std::make_shared<args::Group>(*_program.get(), "global-arguments", args::Group::Validators::DontCare, args::Options::Global)),
		program(_program),
		commandGroup(std::make_shared<args::Group>(*_program.get(), "commands", args::Group::Validators::DontCare)),
		helpFlag(std::make_shared<args::HelpFlag>(*group.get(), "help", "Display this help menu", args::Matcher{'h', "help"})) { }

	ArgsParser(std::string name) :
		ArgsParser(std::make_shared<args::ArgumentParser>(name)) { }

	void setHelpWidth(int width) const {
		program->helpParams.width = width;
	}

	std::shared_ptr<ArgsParserCommand> addCommand(std::string name, std::string description, std::function<int()> onMatched) {
		std::shared_ptr<args::Command> command = std::make_shared<args::Command>(*commandGroup.get(), name, description);
		std::shared_ptr<ArgsParserCommand> commandParser = std::make_shared<ArgsParserCommand>(command);
		commandParser->setAction(onMatched);
		commandGroupArguments.emplace_back(commandParser);
		return commandParser;
	}

	void setAction(std::function<int()> _action) {
		action = _action;
		program->RequireCommand(false);
	}

	int parse(const int argc, const char* const* argv) {

		try {
			program->ParseCLI(argc, argv);
			for (auto& parser : arguments) {
				std::function<void()> func = std::get<1>(parser);
				func();
			}
		}
		catch (args::Help) {
			std::cout << program->Help();
			return 0;
		}
		catch (args::ParseError e) {
			std::cerr << e.what() << std::endl;
			std::cerr << program->Help();
			return 1;
		}
		catch (args::ValidationError e) {
			std::cerr << e.what() << std::endl;
			std::cerr << program->Help();
			return 1;
		}
		catch (std::runtime_error e) {
			std::cerr << e.what() << std::endl;
			std::cerr << program->Help();
			return 1;
		}

		for (auto& commandGroupArgument : commandGroupArguments) {
			if (commandGroupArgument->group->Matched()) {
				try {
					return commandGroupArgument->parse();
				}
				catch (std::runtime_error e) {
					std::cerr << e.what() << std::endl;
					std::cerr << program->Help();
					return 1;
				}
			}
		}

		return action();
	}
};
