﻿using System;
using System.Xml.Linq;
using HA4IoT.Actuators;
using HA4IoT.Contracts.Actuators;
using HA4IoT.Contracts.Configuration;
using HA4IoT.Contracts.Core;
using HA4IoT.Contracts.Hardware;
using HA4IoT.Hardware;
using HA4IoT.Hardware.OpenWeatherMapWeatherStation;

namespace HA4IoT.Configuration
{
    public class DefaultConfigurationExtender : ConfigurationExtenderBase
    {
        public DefaultConfigurationExtender(ConfigurationParser parser, IController controller) : base(parser, controller)
        {
            if (parser == null) throw new ArgumentNullException(nameof(parser));
            if (controller == null) throw new ArgumentNullException(nameof(controller));

            Namespace = string.Empty;
        }

        public override IDevice ParseDevice(XElement element)
        {
            switch (element.Name.LocalName)
            {
                case "I2CBus": return ParseI2CBus(element);
                case "OWMWeatherStation": return ParseWeatherStation(element);

                default: throw new ConfigurationInvalidException("Device not supported.", element);
            }
        }

        public override IActuator ParseActuator(XElement element)
        {
            switch (element.Name.LocalName)
            {
                case "BinaryStateOutputActuator": return ParseBinaryStateOutputActuator(element); 
                case "Lamp": return ParseLamp(element);
                case "Socket": return ParseSocket(element);
                case "Button": return ParseButton(element);
                case "RollerShutter": return ParseRollerShutter(element);
                case "RollerShutterButtons": return ParseRollerShutterButtons(element);
                case "Window": return ParseWindow(element);
                case "TemperatureSensor": return ParseTemperatureSensor(element);
                case "HumiditySensor": return ParseHumiditySensor(element);
                case "StateMachine": return ParseStateMachine(element);

                default: throw new ConfigurationInvalidException("Actuator not supported.", element);
            }
        }

        private IDevice ParseI2CBus(XElement element)
        {
            return new DefaultI2CBus(new DeviceId(element.GetMandatoryStringFromAttribute("id")), Controller.Logger);
        }

        private IDevice ParseWeatherStation(XElement element)
        {
            return new OWMWeatherStation(
                new DeviceId(element.GetMandatoryStringFromAttribute("id")), 
                element.GetMandatoryDoubleFromAttribute("lat"),
                element.GetMandatoryDoubleFromAttribute("lon"),
                element.GetMandatoryStringFromAttribute("appId"),
                Controller.Timer,
                Controller.HttpApiController,
                Controller.Logger);
        }

        private IActuator ParseBinaryStateOutputActuator(XElement element)
        {
            IBinaryOutput output = Parser.ParseBinaryOutput(element.GetMandatorySingleChildElementOrFromContainer("Output"));

            return new BinaryStateOutputActuator(
                new ActuatorId(element.GetMandatoryStringFromAttribute("id")),
                output,
                Controller.HttpApiController,
                Controller.Logger);
        }

        private IActuator ParseButton(XElement element)
        {
            IBinaryInput input = Parser.ParseBinaryInput(element.GetMandatorySingleChildElementOrFromContainer("Input"));

            return new Button(
                new ActuatorId(element.GetMandatoryStringFromAttribute("id")),
                input,
                Controller.HttpApiController,
                Controller.Logger,
                Controller.Timer);
        }

        private IActuator ParseSocket(XElement element)
        {
            IBinaryOutput output = Parser.ParseBinaryOutput(element.GetMandatorySingleChildElementOrFromContainer("Output"));

            return new Socket(
                new ActuatorId(element.GetMandatoryStringFromAttribute("id")),
                output,
                Controller.HttpApiController,
                Controller.Logger);
        }

        private IActuator ParseLamp(XElement element)
        {
            IBinaryOutput output = Parser.ParseBinaryOutput(element.GetMandatorySingleChildElementOrFromContainer("Output"));

            return new Lamp(
                new ActuatorId(element.GetMandatoryStringFromAttribute("id")),
                output,
                Controller.HttpApiController,
                Controller.Logger);
        }

        private IActuator ParseRollerShutter(XElement element)
        {
            IBinaryOutput powerOutput = Parser.ParseBinaryOutput(element.GetMandatorySingleChildFromContainer("Power"));
            IBinaryOutput directionOutput = Parser.ParseBinaryOutput(element.GetMandatorySingleChildFromContainer("Direction"));

            return new RollerShutter(
                new ActuatorId(element.GetMandatoryStringFromAttribute("id")),
                powerOutput,
                directionOutput,
                element.GetTimeSpanFromAttribute("autoOffTimeout", TimeSpan.FromSeconds(22)),
                element.GetIntFromAttribute("maxPosition", 20000),
                Controller.HttpApiController,
                Controller.Logger,
                Controller.Timer);
        }

        private IActuator ParseRollerShutterButtons(XElement element)
        {
            IBinaryInput upInput = Parser.ParseBinaryInput(element.GetMandatorySingleChildFromContainer("Up"));
            IBinaryInput downInput = Parser.ParseBinaryInput(element.GetMandatorySingleChildFromContainer("Down"));

            return new RollerShutterButtons(
                new ActuatorId(element.GetMandatoryStringFromAttribute("id")),
                upInput,
                downInput,
                Controller.HttpApiController,
                Controller.Logger,
                Controller.Timer);
        }

        private IActuator ParseWindow(XElement element)
        {
            var window = new Window(
                new ActuatorId(element.GetMandatoryStringFromAttribute("id")),
                Controller.HttpApiController,
                Controller.Logger);

            var leftCasementElement = element.Element("LeftCasement");
            if (leftCasementElement != null)
            {
                window.WithCasement(ParseCasement(leftCasementElement, Casement.LeftCasementId));
            }

            var centerCasementElement = element.Element("CenterCasement");
            if (centerCasementElement != null)
            {
                window.WithCasement(ParseCasement(centerCasementElement, Casement.CenterCasementId));
            }

            var rightCasementElement = element.Element("RightCasement");
            if (rightCasementElement != null)
            {
                window.WithCasement(ParseCasement(rightCasementElement, Casement.RightCasementId));
            }

            return window;
        }

        private Casement ParseCasement(XElement element, string defaultId)
        {
            IBinaryInput fullOpenInput = Parser.ParseBinaryInput(element.GetMandatorySingleChildFromContainer("FullOpen"));

            IBinaryInput tiltInput = null;
            if (element.HasChildElement("Tilt"))
            {
                tiltInput = Parser.ParseBinaryInput(element.GetMandatorySingleChildFromContainer("Tilt"));
            }
            
            var casement = new Casement(element.GetStringFromAttribute("id", defaultId), fullOpenInput, tiltInput);
            return casement;
        }

        private IActuator ParseHumiditySensor(XElement element)
        {
            ISingleValueSensor sensor = Parser.ParseSingleValueSensor(element.GetMandatorySingleChildElementOrFromContainer("Sensor"));

            return new HumiditySensor(
                new ActuatorId(element.GetMandatoryStringFromAttribute("id")),
                sensor,
                Controller.HttpApiController,
                Controller.Logger);
        }

        private IActuator ParseTemperatureSensor(XElement element)
        {
            ISingleValueSensor sensor = Parser.ParseSingleValueSensor(element.GetMandatorySingleChildElementOrFromContainer("Sensor"));

            return new TemperatureSensor(
                new ActuatorId(element.GetMandatoryStringFromAttribute("id")),
                sensor,
                Controller.HttpApiController,
                Controller.Logger);
        }

        private IActuator ParseStateMachine(XElement element)
        {
            var id = new ActuatorId(element.GetMandatoryStringFromAttribute("id"));

            var stateMachine = new StateMachine(id, Controller.HttpApiController, Controller.Logger);

            foreach (var stateElement in element.Element("States").Elements("State"))
            {
                var stateId = stateElement.GetMandatoryStringFromAttribute("id");
                var state = stateMachine.AddState(stateId);
                
                foreach (var lowPortElement in stateElement.Element("LowPorts").Elements())
                {
                    state.WithPort(Parser.ParseBinaryOutput(lowPortElement), BinaryState.Low);
                }

                foreach (var highPortElement in stateElement.Element("LowPorts").Elements())
                {
                    state.WithPort(Parser.ParseBinaryOutput(highPortElement), BinaryState.High);
                }

                foreach (var actuatorElement in stateElement.Element("Actuators").Elements())
                {
                    var targetState = actuatorElement.GetMandatoryEnumFromAttribute<BinaryActuatorState>("targetState");
                    var actuatorId = new ActuatorId(actuatorElement.GetMandatoryStringFromAttribute("id"));
                    var actuator = Controller.Actuator<IBinaryStateOutputActuator>(actuatorId);

                    state.WithActuator(actuator, targetState);
                }
            }
            
            return stateMachine;
        }
    }
}